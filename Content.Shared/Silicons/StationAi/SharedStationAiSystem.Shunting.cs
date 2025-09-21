using Content.Shared.DoAfter;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Power;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private const string ShuntingContainer = "shunting";

    private void InitializeShunting()
    {
        SubscribeLocalEvent<StationAiShuntingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiShuntingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiShuntingEvent>(OnShunt);
        SubscribeLocalEvent<StationAiShuntingComponent, PowerChangedEvent>((u, c, a) => OnPowerChange(u, c, a.Powered));
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiHackedEvent>(OnHacked);

        SubscribeLocalEvent<StationAiCanShuntComponent, MoveInputEvent>(OnMoveAttempt);
    }

    private void OnInit(EntityUid uid, StationAiShuntingComponent component, ComponentInit args)
    {
        _containers.EnsureContainer<ContainerSlot>(uid,ShuntingContainer);
    }

    private void OnAttempt(EntityUid uid, StationAiShuntingComponent component, StationAiShuntingAttemptEvent args)
    {
        if (!component.IsPowered || !component.Enabled)
            return;

        if (!TryComp<StationAiCanShuntComponent>(args.User, out var canShuntComp))
            return;

        if (canShuntComp.ShuntedContainer != null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.Delay, new StationAiShuntingEvent(), uid, uid, progressBarOverride: uid));
    }

    private void OnShunt(EntityUid uid, StationAiShuntingComponent component, StationAiShuntingEvent args)
    {
        if (!component.IsPowered || !component.Enabled)
            return;

       if (!TryComp<StationAiCanShuntComponent>(args.User, out var canShuntComp))
           return;

       if (canShuntComp.ShuntedContainer != null)
           return;

        if (!_containers.TryGetContainer(uid, ShuntingContainer, out var container))
            return;

        var hasAiOverlay = HasComp<StationAiOverlayComponent>(args.User);

        if (_containers.TryGetContainingContainer(args.User, out var startingContainer))
        {
            canShuntComp.Container = startingContainer;
        }
        else
        {
            canShuntComp.Container = null;
        }

        canShuntComp.ShuntedContainer = container;
        _containers.Insert(args.User, container);

        if (TryComp<EyeComponent>(args.User, out var eyeComp))
        {
            canShuntComp.DrawFoV = eyeComp.DrawFov;
            Dirty(args.User, canShuntComp);
            _eye.SetDrawFov(args.User, false, eyeComp);
        }
        else
        {
            canShuntComp.DrawFoV = false;
            Dirty(args.User, canShuntComp);
        }

        if (!hasAiOverlay)
            return;

        EnsureComp<StationAiOverlayComponent>(args.User);
    }

    private void OnMoveAttempt(EntityUid uid, StationAiCanShuntComponent component, ref MoveInputEvent args)
    {
        if ((args.Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        Eject(uid, component);
    }

    protected void Eject(EntityUid uid, StationAiCanShuntComponent? component = null, bool force = false)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.ShuntedContainer == null)
            return;

        if (component.Container != null)
        {
            var ev = new StationAiShuntingEjectAttemptEvent(uid);
            RaiseLocalEvent(component.Container.Owner, ev);

            if (ev.Cancelled && !force)
                return;

            if (!_containers.Insert(uid, component.Container) && !force)
                return;
        }

        if (TryComp<EyeComponent>(uid, out var eyeComp))
            _eye.SetDrawFov(uid, component.DrawFoV, eyeComp);

        if (TryGetCore(uid, out var core) && core.Comp?.RemoteEntity != null)
            _xforms.DropNextTo(core.Comp.RemoteEntity.Value, component.ShuntedContainer.Owner);

        component.ShuntedContainer = null;
        Dirty(uid, component);
    }

    public void OnPowerChange(EntityUid uid, StationAiShuntingComponent component, bool isPowered)
    {
        if (component.IsPowered == isPowered)
            return;

        component.IsPowered = isPowered;
        Dirty(uid, component);

        if (isPowered)
            return;

        if (!_containers.TryGetContainer(uid, ShuntingContainer, out var container))
            return;

        foreach (var containedEntity in container.ContainedEntities)
        {
            Eject(containedEntity, force: true);
        }
    }

    private void OnHacked(EntityUid uid, StationAiShuntingComponent component, StationAiHackedEvent args)
    {
        component.Enabled = true;
        Dirty(uid, component);
    }
}
