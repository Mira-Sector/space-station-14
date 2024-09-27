using Content.Server.DeviceLinking.Systems;
using Content.Shared.Actions;
using Content.Shared.Vehicles;

namespace Content.Server.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleSignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VehicleSignallerComponent, HornActionEvent>(OnHorn);
    }

    private void OnInit(EntityUid uid, VehicleSignallerComponent component, ComponentInit args)
    {
        _link.EnsureSourcePorts(uid, component.Port);
    }

    private void OnHorn(EntityUid uid, VehicleSignallerComponent component, InstantActionEvent args)
    {
        if (args.Handled == true)
            return;

        if (component.Port == null)
            return;

        _link.InvokePort(uid, component.Port);
    }
}
