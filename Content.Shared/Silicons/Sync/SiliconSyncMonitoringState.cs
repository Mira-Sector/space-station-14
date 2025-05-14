using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync;

[Serializable, NetSerializable]
public sealed class SiliconSyncMonitoringState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, HashSet<NetEntity>> MasterSlaves;
    public Dictionary<NetEntity, ProtoId<NavMapBlipPrototype>> SlaveBlips;

    public SiliconSyncMonitoringState(Dictionary<NetEntity, HashSet<NetEntity>> masterSlaves, Dictionary<NetEntity, ProtoId<NavMapBlipPrototype>> slaveBlips)
    {
        MasterSlaves = masterSlaves;
        SlaveBlips = slaveBlips;
    }
}
