using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Events;
using Content.Shared.Surgery.UI;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private void InitializeUI()
    {
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, StrappedEvent>(OnLinkedSourceStrapped);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, UnstrappedEvent>(OnLinkedSourceUnstrapped);

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, GetSurgeryUiTargetEvent>(OnLinkedSinkGetTarget, before: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<SurgeryUserInterfaceSourceRangeComponent, GetSurgeryUiTargetEvent>(OnSourceRangeGetTarget, before: [typeof(SharedBuckleSystem)]);

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, GetSurgeryUiSourceEvent>(OnLinkedSinkGetSource);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, GetSurgeryUiSourceEvent>(OnLinkedSourceGetSource);

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, NewLinkEvent>((u, c, a) => OnNewLink(a));
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, NewLinkEvent>((u, c, a) => OnNewLink(a));

        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, PortDisconnectedEvent>(OnSinkDisconnected);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, PortDisconnectedEvent>(OnSourceDisconnected);

        SubscribeLocalEvent<SurgeryCurrentNodeModifiedEvent>(OnNodeModified);
        SubscribeLocalEvent<SurgeryBodyCurrentNodeModifiedEvent>(OnBodyNodeModified);

        Subs.BuiEvents<SurgeryUserInterfaceComponent>(SurgeryUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
        });
    }

    private void OnUiOpened(Entity<SurgeryUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {
        TryGetTarget(ent.Owner, out var target);
        UpdateUi(ent.Owner, target);
    }

    private void OnLinkedSourceStrapped(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref StrappedEvent args)
    {
        if (ent.Comp.Sink is not { } sink)
            return;

        UpdateUi(sink, args.Buckle.Owner);
    }

    private void OnLinkedSourceUnstrapped(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.Sink is not { } sink)
            return;

        UpdateUi(sink, null);
    }

    private void OnLinkedSinkGetTarget(Entity<SurgeryUserInterfaceLinkedSinkComponent> ent, ref GetSurgeryUiTargetEvent args)
    {
        if (ent.Comp.Source is not { } source)
            return;

        if (!TryGetTarget(source, out var target))
            return;

        args.Target = target;
    }

    private void OnSourceRangeGetTarget(Entity<SurgeryUserInterfaceSourceRangeComponent> ent, ref GetSurgeryUiTargetEvent args)
    {
        if (args.Target != null)
            return;

        EntityUid? closestMatch = null;
        var closestPosSquared = float.MaxValue;

        foreach (var entity in _lookup.GetEntitiesInRange<SurgeryReceiverComponent>(Transform(ent.Owner).Coordinates, ent.Comp.Range, SurgeryUserInterfaceSourceRangeComponent.Flags))
        {
            var pos = Vector2.Abs(Transform(entity).LocalPosition);
            var posSquared = pos.LengthSquared();

            if (posSquared > closestPosSquared)
                continue;

            closestPosSquared = posSquared;
            closestMatch = entity;
        }

        args.Target = closestMatch;
    }

    private void OnLinkedSinkGetSource(Entity<SurgeryUserInterfaceLinkedSinkComponent> ent, ref GetSurgeryUiSourceEvent args)
    {
        args.Source = ent.Owner;
    }

    private void OnLinkedSourceGetSource(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref GetSurgeryUiSourceEvent args)
    {
        if (ent.Comp.Sink is not { } sink)
            return;

        args.Source = sink;
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

    private void OnNodeModified(ref SurgeryCurrentNodeModifiedEvent args)
    {
        if (TryGetUiEntity(args.Receiver, out var ui))
        {
            UpdateUi(ui.Value, args.Receiver);
            return;
        }

        SourceRangeUpdateUi(args.Receiver);
    }

    private void OnBodyNodeModified(ref SurgeryBodyCurrentNodeModifiedEvent args)
    {
        if (args.Body == null)
            return;

        if (TryGetUiEntity(args.Body.Value, out var ui))
        {
            UpdateUi(ui.Value, args.Body.Value);
            return;
        }

        SourceRangeUpdateUi(args.Body.Value);
    }

    private void SourceRangeUpdateUi(EntityUid target)
    {
        var targetPos = Transform(target).Coordinates;

        var query = EntityQueryEnumerator<SurgeryUserInterfaceSourceRangeComponent>();
        while (query.MoveNext(out var sourceRangeUid, out var sourceRangeComp))
        {
            var sourceRangePos = Transform(sourceRangeUid).Coordinates;

            if (!_transform.InRange(sourceRangePos, targetPos, sourceRangeComp.Range))
                continue;

            if (TryGetUiEntity(sourceRangeUid, out var ui))
                UpdateUi(ui.Value, target);
        }
    }

    private void UpdateUi(EntityUid ui, EntityUid? target)
    {
        var state = new SurgeryBoundUserInterfaceState(GetNetEntity(target));
        Ui.SetUiState(ui, SurgeryUiKey.Key, state);
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
