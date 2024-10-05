using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingEnterPointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, AnchorStateChangedEvent>(OnAnchored);

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, GetVerbsEvent<ActivationVerb>>(OnVerb);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, ActivateInWorldEvent>(OnInteract);
    }

    private void OnInit(EntityUid uid, PipeCrawlingEnterPointComponent component, ref ComponentInit args)
    {
        UpdateState(uid, component);
    }

    private void OnAnchored(EntityUid uid, PipeCrawlingEnterPointComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, PipeCrawlingEnterPointComponent component)
    {
        component.Enterable = Comp<TransformComponent>(uid).Anchored & component.CanEnter;
        component.Exitable = Comp<TransformComponent>(uid).Anchored & component.CanExit;
    }

    private void OnVerb(EntityUid uid, PipeCrawlingEnterPointComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!HasComp<CanEnterPipeCrawlingComponent>(args.User))
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        switch (pipeComp.ContainedEntities.Contains(args.User))
        {
            case true:
            {
                if (!component.Exitable)
                    return;

                args.Verbs.Add(new ActivationVerb()
                {
                    Text = Loc.GetString("connecting-exit"),
                    Act = () =>
                    {
                        PipeExit(args.User, uid);
                    }
                });

                break;
            }

            case false:
            {
                if (!component.Enterable)
                    return;

                args.Verbs.Add(new ActivationVerb()
                {
                    Text = Loc.GetString("mech-verb-enter"),
                    Act = () =>
                    {
                        PipeEnter(args.User, uid);
                    }
                });

                break;
            }
        }
    }

    private void OnInteract(EntityUid uid, PipeCrawlingEnterPointComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        switch (pipeComp.ContainedEntities.Contains(args.User))
        {
            case true:
            {
                if (!component.Exitable)
                    return;

                PipeExit(args.User, uid);
                break;
            }

            case false:
            {
                if (!component.Enterable)
                    return;

                PipeEnter(args.User, uid);
                break;
            }
        }

        args.Handled = true;
    }

    private void PipeEnter(EntityUid user, EntityUid pipe)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(pipe, out var pipeComp))
            return;

        if (pipeComp.ContainedEntities.Contains(user))
            return;

        _xform.TryGetMapOrGridCoordinates(pipe, out var pipePos);
        var pipeRot = _xform.GetWorldRotation(pipe).GetDir();

        if (pipePos == null)
            return;

        pipeComp.ContainedEntities.Add(user);
        var pipeCrawlComp = EnsureComp<PipeCrawlingComponent>(user);
        pipeCrawlComp.CurrentPipe = pipe;
        pipeCrawlComp.NextMoveAttempt = TimeSpan.Zero;

        _xform.SetCoordinates(user, pipePos.Value);
        Dirty(user, pipeCrawlComp);
    }

    private void PipeExit(EntityUid user, EntityUid pipe)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(pipe, out var pipeComp))
            return;

        if (!pipeComp.ContainedEntities.Contains(user))
            return;

        pipeComp.ContainedEntities.Remove(user);
        RemComp<PipeCrawlingComponent>(user);
    }
}
