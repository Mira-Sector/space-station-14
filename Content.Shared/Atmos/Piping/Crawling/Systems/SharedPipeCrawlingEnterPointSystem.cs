using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public sealed class SharedPipeCrawlingEnterPointSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;

    const string PipeContainer = "pipe";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, AnchorStateChangedEvent>(OnAnchored);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, WeldableChangedEvent>(OnWelded);

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, GetVerbsEvent<ActivationVerb>>(OnVerb);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, ActivateInWorldEvent>(OnInteract);

        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, PipeEnterDoAfterEvent>(OnDoAfter);
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

        if (!TryComp<CanEnterPipeCrawlingComponent>(args.User, out var canEnterPipeComp))
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        if (!_containers.TryGetContainer(uid, PipeContainer, out var pipeContainer))
            return;

        var pipeCrawling = HasComp<PipeCrawlingComponent>(args.User);

        var verb = new ActivationVerb();

        switch (InPipe(args.User, pipeContainer))
        {
            case true:
            {
                if (!component.Exitable)
                    return;

                if (!pipeCrawling)
                    return;

                verb.Text = Loc.GetString("connecting-exit");
                verb.Act = () =>
                {
                    PipeExit(args.User, uid, pipeContainer);
                };

                break;
            }

            case false:
            {
                if (!component.Enterable)
                    return;

                if (pipeCrawling)
                    return;

                verb.Text = Loc.GetString("mech-verb-enter");
                verb.Act = () =>
                {
                    PipeEnter(args.User, uid, canEnterPipeComp);
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

        if (!TryComp<CanEnterPipeCrawlingComponent>(args.User, out var canEnterPipeComp))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<PipeCrawlingPipeComponent>(uid, out var pipeComp))
            return;

        if (!_containers.TryGetContainer(uid, PipeContainer, out var pipeContainer))
            return;

        var pipeCrawling = HasComp<PipeCrawlingComponent>(args.User);

        switch (InPipe(args.User, pipeContainer))
        {
            case true:
            {
                if (!component.Exitable)
                    break;

                if (!pipeCrawling)
                    break;

                PipeExit(args.User, uid, pipeContainer);
                break;
            }

            case false:
            {
                if (!component.Enterable)
                    break;

                if (pipeCrawling)
                    break;

                PipeEnter(args.User, uid, canEnterPipeComp);
                break;
            }
        }

        args.Handled = true;
    }

    private bool InPipe(EntityUid user, BaseContainer pipeContainer)
    {
        foreach (var ent in pipeContainer.ContainedEntities)
        {
            if (ent != user)
                continue;

            return true;
        }

        return false;
    }

    private void PipeEnter(EntityUid user, EntityUid pipe, CanEnterPipeCrawlingComponent canPipeCrawlComp)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, canPipeCrawlComp.EnterPipeDoAfterTime , new PipeEnterDoAfterEvent(), pipe, pipe)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        });
    }

    private void OnDoAfter(EntityUid pipe, PipeCrawlingEnterPointComponent component, PipeEnterDoAfterEvent args)
    {
        if (!_containers.TryGetContainer(pipe, PipeContainer, out var pipeContainer))
            return;

        _containers.Insert(args.User, pipeContainer);
        var pipeCrawlComp = EnsureComp<PipeCrawlingComponent>(args.User);
        pipeCrawlComp.CurrentPipe = pipe;
        pipeCrawlComp.NextMoveAttempt = TimeSpan.Zero;

        Dirty(args.User, pipeCrawlComp);
    }

    private void PipeExit(EntityUid user, EntityUid pipe, BaseContainer pipeContainer)
    {
        if (!TryComp<PipeCrawlingPipeComponent>(pipe, out var pipeComp))
            return;

        _containers.Remove(user, pipeContainer);
        RemComp<PipeCrawlingComponent>(user);
    }
}
