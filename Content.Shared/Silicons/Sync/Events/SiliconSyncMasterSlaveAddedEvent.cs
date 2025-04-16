namespace Content.Shared.Silicons.Sync.Events;

public sealed partial class SiliconSyncMasterSlaveAddedEvent : BaseSiliconSyncMasterEvent
{
    public SiliconSyncMasterSlaveAddedEvent(EntityUid slave) : base(slave)
    {
    }
}
