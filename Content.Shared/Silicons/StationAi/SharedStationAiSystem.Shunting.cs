using Content.Shared.DoAfter;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Power;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.StationAi;


public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafterSystem = default!;

    private const string ShuntingContainer = "shunting";

    private void InitializeShunting()
    {
        SubscribeLocalEvent<StationAiShuntingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiShuntingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiShuntingEvent>(OnShunt);
        SubscribeLocalEvent<StationAiShuntingComponent, PowerChangedEvent>((u, c, a) => OnPowerChange(u, c, a.Powered));
        SubscribeLocalEvent<StationAiCanShuntComponent, MoveInputEvent>(OnMoveAttempt);
    }

    private void OnInit(EntityUid uid, StationAiShuntingComponent component, ComponentInit args)
    {
        _containers.EnsureContainer<ContainerSlot>(uid,ShuntingContainer);
    }

    private void OnAttempt(EntityUid uid, StationAiShuntingComponent component, StationAiShuntingAttemptEvent args)
    {
        if (!component.IsPowered)
            return;

        if (!TryComp<StationAiCanShuntComponent>(args.User, out var canShuntComp))
            return;

        if (canShuntComp.ShuntedContainer != null)
            return;

        _doafterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.Delay, new StationAiShuntingEvent(), uid, uid, progressBarOverride: uid));
    }

    private void OnShunt(EntityUid uid, StationAiShuntingComponent component, StationAiShuntingEvent args)
    {
        if (!component.IsPowered)
            return;

       if (!TryComp<StationAiCanShuntComponent>(args.User, out var canShuntComp))
           return;

       if (canShuntComp.ShuntedContainer != null)
           return;

        if (!_containers.TryGetContainer(uid, ShuntingContainer, out var container))
            return;

        if (_containers.TryGetContainingContainer(args.User, out var startingContainer))
        {
            canShuntComp.Container = startingContainer;
            _containers.RemoveEntity(startingContainer.Owner, args.User);
        }
        else
        {
            canShuntComp.Container = null;
        }

        canShuntComp.ShuntedContainer = container;
        Dirty(args.User, canShuntComp);
        _containers.Insert(args.User, container);
    }

    private void OnMoveAttempt(EntityUid uid, StationAiCanShuntComponent component, ref MoveInputEvent args)
    {
        if (component.ShuntedContainer == null)
            return;

        if ((args.Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        _containers.RemoveEntity(component.ShuntedContainer.Owner, uid);

        component.ShuntedContainer = null;
        Dirty(uid, component);

        if (component.Container != null)
            _containers.Insert(uid, component.Container);
    }

    public void OnPowerChange(EntityUid uid, StationAiShuntingComponent component, bool isPowered)
    {
        component.IsPowered = isPowered;
        Dirty(uid, component);

        if (isPowered)
            return;

        if (!_containers.TryGetContainer(uid, ShuntingContainer, out var container))
            return;

        foreach (var containedEntity in container.ContainedEntities)
        {
            _containers.RemoveEntity(uid, containedEntity);

            if (!TryComp<StationAiCanShuntComponent>(containedEntity, out var canShuntComp))
                continue;

            if (canShuntComp.Container == null)
                continue;

            _containers.Insert(containedEntity, canShuntComp.Container);

            canShuntComp.ShuntedContainer = null;
            Dirty(containedEntity, canShuntComp);
        }
    }
}
