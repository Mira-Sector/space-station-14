using Content.Server.Wires;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Wires;

namespace Content.Server.Silicons.Sync;

public sealed partial class SiliconSyncLawWireAction : ComponentWireAction<SiliconSyncableSlaveLawComponent>
{
    public override string Name { get; set; } = "wire-name-silicon-law";
    public override Color Color { get; set; } = Color.RosyBrown;
    public override object StatusKey => SiliconSyncWireStatus.Law;

    public override StatusLightState? GetLightState(Wire wire, SiliconSyncableSlaveLawComponent component)
    {
        return component.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, SiliconSyncableSlaveLawComponent component)
    {
        component.Enabled = false;
        EntityManager.Dirty(wire.Owner, component);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SiliconSyncableSlaveLawComponent component)
    {
        component.Enabled = true;
        EntityManager.Dirty(wire.Owner, component);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SiliconSyncableSlaveLawComponent component)
    {
        EntityManager.System<SharedSiliconSyncSystem>().UpdateSlaveLaws((wire.Owner, null, component));
    }
}
