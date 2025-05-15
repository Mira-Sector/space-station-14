using Content.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync;

[Serializable, NetSerializable]
public sealed class SiliconSyncMonitoringState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, Dictionary<NetEntity, NetCoordinates>> MasterSlaves;
    public Dictionary<NetEntity, ProtoId<NavMapBlipPrototype>> SlaveBlips;

    public SiliconSyncMonitoringState(Dictionary<NetEntity, Dictionary<NetEntity, NetCoordinates>> masterSlaves, Dictionary<NetEntity, ProtoId<NavMapBlipPrototype>> slaveBlips)
    {
        MasterSlaves = masterSlaves;
        SlaveBlips = slaveBlips;
    }
}
