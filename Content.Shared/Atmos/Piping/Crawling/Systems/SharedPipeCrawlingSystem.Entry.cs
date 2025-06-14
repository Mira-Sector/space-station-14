using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public partial class SharedPipeCrawlingSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private void InitializeEntry()
    {
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, ComponentInit>(OnEntryInit);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, WeldableChangedEvent>(OnEntryWelded);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, AnchorStateChangedEvent>(OnEntryAnchor);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, GetVerbsEvent<ActivationVerb>>(OnEntryVerbs);
        SubscribeLocalEvent<PipeCrawlingEnterPointComponent, PipeCrawlingEnterDoAfterEvent>(OnEnterDoAfter);
    }

    private void OnEntryInit(Entity<PipeCrawlingEnterPointComponent> ent, ref ComponentInit args)
    {
        var disabled = false;

        if (TryComp<WeldableComponent>(ent.Owner, out var weldable))
            disabled |= weldable.IsWelded;

        disabled |= !Transform(ent.Owner).Anchored;

        ent.Comp.Enterable = ent.Comp.CanEnter & !disabled;
        ent.Comp.Exitable = ent.Comp.CanExit & !disabled;
        Dirty(ent);
    }

    private void OnEntryWelded(Entity<PipeCrawlingEnterPointComponent> ent, ref WeldableChangedEvent args)
    {
        var isAnchored = Transform(ent.Owner).Anchored;
        ent.Comp.Enterable = ent.Comp.CanEnter & isAnchored & args.IsWelded;
        ent.Comp.Exitable = ent.Comp.CanEnter & isAnchored & args.IsWelded;
        Dirty(ent);
    }

    private void OnEntryAnchor(Entity<PipeCrawlingEnterPointComponent> ent, ref AnchorStateChangedEvent args)
    {
        var isWelded = CompOrNull<WeldableComponent>(ent.Owner)?.IsWelded ?? false;
        ent.Comp.Enterable = ent.Comp.CanEnter & isWelded & args.Anchored;
        ent.Comp.Exitable = ent.Comp.CanEnter & isWelded & args.Anchored;
        Dirty(ent);
    }

    private void OnEntryVerbs(Entity<PipeCrawlingEnterPointComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        if (!HasComp<CanEnterPipeCrawlingComponent>(user))
            return;

        var pipeComp = Comp<PipeCrawlingPipeComponent>(ent.Owner);
        var isCrawling = HasComp<PipeCrawlingComponent>(user);
        var contains = pipeComp.Container.Contains(user);

        var disabled = false;

        if (isCrawling)
        {
            disabled |= !ent.Comp.CanExit;
            disabled |= !contains;
        }
        else
        {
            disabled |= !ent.Comp.CanEnter;
        }

        var verb = new ActivationVerb()
        {
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/vent.svg.192dpi.png")),
            DoContactInteraction = true,
            Text = isCrawling ? Loc.GetString("pipe-crawling-verb-exit") : Loc.GetString("pipe-crawling-verb-enter"),
            Disabled = disabled,
            Act = () => _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.DoAfterTime, new PipeCrawlingEnterDoAfterEvent(), ent.Owner))
        };

        args.Verbs.Add(verb);
    }

    private void OnEnterDoAfter(Entity<PipeCrawlingEnterPointComponent> ent, ref PipeCrawlingEnterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!HasComp<CanEnterPipeCrawlingComponent>(args.User))
            return;

        var pipeComp = Comp<PipeCrawlingPipeComponent>(ent.Owner);

        var isCrawling = HasComp<PipeCrawlingComponent>(args.User);
        if (isCrawling)
            Eject((ent.Owner, pipeComp), args.User);
        else
            Insert((ent.Owner, pipeComp), args.User);

        args.Handled = true;
    }
}
