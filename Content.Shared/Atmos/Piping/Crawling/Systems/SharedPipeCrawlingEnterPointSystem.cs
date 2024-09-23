using Content.Shared.Atmos.Piping.Crawling.Components;
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

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, GetVerbsEvent<ActivationVerb>>(OnPipeEnterVerb);
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

    private void OnPipeEnterVerb(EntityUid uid, PipeCrawlingEnterPointComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        if (component.Enterable && !pipeComp.ContainedEntities.Contains(args.User))
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    PipeEnter(args.User, uid);
                }
            });
        }

        if (component.Exitable && pipeComp.ContainedEntities.Contains(args.User))
        {
            args.Verbs.Add(new ActivationVerb()
            {
                Text = Loc.GetString("connecting-exit"),
                Act = () =>
                {
                    PipeExit(args.User, uid);
                }
            });
        }
    }

    private void PipeEnter(EntityUid user, EntityUid pipe)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(pipe, out var pipeComp))
            return;

        if (!pipeComp.Enabled)
            return;

        pipeComp.ContainedEntities.Add(user);
        var pipeCrawlComp = EnsureComp<PipeCrawlingComponent>(user);
        pipeCrawlComp.CurrentPipe = pipe;

        _xform.TryGetMapOrGridCoordinates(pipe, out var pipePos);

        if (pipePos == null)
            return;

        _xform.SetCoordinates(user, pipePos.Value);
    }

    private void PipeExit(EntityUid user, EntityUid pipe)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(pipe, out var pipeComp))
            return;

        if (!pipeComp.Enabled)
            return;

        pipeComp.ContainedEntities.Remove(user);
        RemComp<PipeCrawlingComponent>(user);
    }
}
