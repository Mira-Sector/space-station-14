using Content.Server.Wires;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Wires;

namespace Content.Server.Silicons.Sync;

public sealed partial class SiliconSyncWireAction : ComponentWireAction<SiliconSyncableSlaveComponent>
{
    public override string Name { get; set; } = "wire-name-silicon-sync";
    public override Color Color { get; set; } = Color.HotPink;
    public override object StatusKey => SiliconSyncWireStatus.Sync;

    public override StatusLightState? GetLightState(Wire wire, SiliconSyncableSlaveComponent component)
    {
        if (component.Master == null)
        {
            if (wire.IsCut)
                return StatusLightState.Off;
            else
                return StatusLightState.BlinkingSlow;
        }
        else
        {
            return StatusLightState.On;
        }
    }

    public override bool Cut(EntityUid user, Wire wire, SiliconSyncableSlaveComponent component)
    {
        EntityManager.System<SharedSiliconSyncSystem>().SetMaster((wire.Owner, component), null);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SiliconSyncableSlaveComponent component)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SiliconSyncableSlaveComponent component)
    {
        EntityManager.System<SharedSiliconSyncSystem>().ShowAvailableMasters((wire.Owner, component), user);
    }
}
