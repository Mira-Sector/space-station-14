using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Events;
using Content.Shared.Surgery.UI;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    private void InitializeUI()
    {
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, GetSurgeryUiTargetEvent>(OnLinkedSinkGetTarget);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, GetSurgeryUiTargetEvent>(OnLinkedSourceGetTarget);

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, GetSurgeryUiSourceEvent>(OnLinkedSinkGetSource);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, GetSurgeryUiSourceEvent>(OnLinkedSourceGetSource);

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, NewLinkEvent>((u, c, a) => OnNewLink(a));
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, NewLinkEvent>((u, c, a) => OnNewLink(a));

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, PortDisconnectedEvent>(OnSinkDisconnected);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, PortDisconnectedEvent>(OnSourceDisconnected);

        SubscribeLocalEvent<SurgeryReceiverComponent, SurgeryCurrentNodeModifiedEvent>(OnNodeModified);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, SurgeryBodyCurrentNodeModifiedEvent>(OnBodyNodeModified);

        Subs.BuiEvents<SurgeryUserInterfaceComponent>(SurgeryUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
        });
    }

    private void OnUiOpened(Entity<SurgeryUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!TryGetTarget(ent.Owner, out var target))
            return;

        var state = new SurgeryBoundUserInterfaceState(GetNetEntity(target.Value));
        Ui.SetUiState(ent.Owner, args.UiKey, state);
    }

    private void OnLinkedSinkGetTarget(Entity<SurgeryUserInterfaceLinkedSinkComponent> ent, ref GetSurgeryUiTargetEvent args)
    {
        if (ent.Comp.Source is not { } source)
            return;

        if (!TryGetTarget(source, out var target))
            return;

        args.Target = target;
    }

    private void OnLinkedSourceGetTarget(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref GetSurgeryUiTargetEvent args)
    {
        if (!TryComp<StrapComponent>(ent.Owner, out var strap))
            return;

        if (strap.BuckledEntities.Count != 1)
            return;

        args.Target = strap.BuckledEntities.First();
    }

    private void OnLinkedSinkGetSource(Entity<SurgeryUserInterfaceLinkedSinkComponent> ent, ref GetSurgeryUiSourceEvent args)
    {
        args.Source = ent.Owner;
    }

    private void OnLinkedSourceGetSource(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref GetSurgeryUiSourceEvent args)
    {
        if (ent.Comp.Sink is not { } sink)
            return;

        if (!TryGetUiEntity(sink, out var ui))
            return;

        args.Source = ui;
    }

    private void OnNewLink(NewLinkEvent args)
    {
        if (args.SinkPort != SurgeryUserInterfaceLinkedSinkComponent.SinkPort || !TryComp<SurgeryUserInterfaceLinkedSinkComponent>(args.Sink, out var sinkComp))
            return;

        if (args.SourcePort != SurgeryUserInterfaceLinkedSourceComponent.SourcePort || !TryComp<SurgeryUserInterfaceLinkedSourceComponent>(args.Source, out var sourceComp))
            return;

        sinkComp.Source = args.Source;
        sourceComp.Sink = args.Sink;

        Dirty(args.Sink, sinkComp);
        Dirty(args.Source, sourceComp);
    }

    private void OnSinkDisconnected(Entity<SurgeryUserInterfaceLinkedSinkComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != SurgeryUserInterfaceLinkedSinkComponent.SinkPort)
            return;

        ent.Comp.Source = null;
        Dirty(ent);
    }

    private void OnSourceDisconnected(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != SurgeryUserInterfaceLinkedSourceComponent.SourcePort)
            return;

        ent.Comp.Sink = null;
        Dirty(ent);
    }

    private void OnNodeModified(Entity<SurgeryReceiverComponent> ent, ref SurgeryCurrentNodeModifiedEvent args)
    {
        if (!TryGetUiEntity(ent.Owner, out var ui))
            return;
    }

    private void OnBodyNodeModified(Entity<SurgeryReceiverBodyComponent> ent, ref SurgeryBodyCurrentNodeModifiedEvent args)
    {
    }

    public bool TryGetTarget(EntityUid uid, [NotNullWhen(true)] out EntityUid? target)
    {
        var ev = new GetSurgeryUiTargetEvent();
        RaiseLocalEvent(uid, ref ev);

        target = ev.Target;
        return target != null;
    }

    public bool TryGetUiEntity(EntityUid uid, [NotNullWhen(true)] out EntityUid? ui)
    {
        var ev = new GetSurgeryUiSourceEvent();
        RaiseLocalEvent(uid, ref ev);

        ui = ev.Source;
        return ui != null;
    }
}
