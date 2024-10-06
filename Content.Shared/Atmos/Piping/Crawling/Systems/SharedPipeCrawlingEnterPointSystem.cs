using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingEnterPointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, AnchorStateChangedEvent>(OnAnchored);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, WeldableChangedEvent>(OnWelded);

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

    private void OnWelded(EntityUid uid, PipeCrawlingEnterPointComponent component, ref WeldableChangedEvent args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, PipeCrawlingEnterPointComponent component)
    {
        component.Enterable = Comp<TransformComponent>(uid).Anchored & !_weldable.IsWelded(uid) & component.CanEnter;
        component.Exitable = Comp<TransformComponent>(uid).Anchored & !_weldable.IsWelded(uid) &  component.CanExit;
    }

    private void OnVerb(EntityUid uid, PipeCrawlingEnterPointComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!HasComp<CanEnterPipeCrawlingComponent>(args.User))
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        var inPipe = HasComp<PipeCrawlingComponent>(args.User);

        var verb = new ActivationVerb();

        switch (pipeComp.ContainedEntities.Contains(args.User))
        {
            case true:
            {
                if (!component.Exitable)
                    return;

                if (!inPipe)
                    return;

                verb.Text = Loc.GetString("connecting-exit");
                verb.Act = () =>
                {
                    PipeExit(args.User, uid);
                };

                break;
            }

            case false:
            {
                if (!component.Enterable)
                    return;

                if (inPipe)
                    return;

                verb.Text = Loc.GetString("mech-verb-enter");
                verb.Act = () =>
                {
                    PipeEnter(args.User, uid);
                };

                break;
            }
        }

        args.Verbs.Add(verb);
    }
    private void OnInteract(EntityUid uid, PipeCrawlingEnterPointComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<CanEnterPipeCrawlingComponent>(args.User))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        var inPipe = HasComp<PipeCrawlingComponent>(args.User);

        switch (pipeComp.ContainedEntities.Contains(args.User))
        {
            case true:
            {
                if (!component.Exitable)
                    break;

                if (!inPipe)
                    break;

                PipeExit(args.User, uid);
                break;
            }

            case false:
            {
                if (!component.Enterable)
                    break;

                if (inPipe)
                    break;

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
