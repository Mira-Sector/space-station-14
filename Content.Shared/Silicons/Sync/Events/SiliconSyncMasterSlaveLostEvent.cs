namespace Content.Shared.Silicons.Sync.Events;

public sealed partial class SiliconSyncMasterSlaveLostEvent : BaseSiliconSyncMasterEvent
{
    public SiliconSyncMasterSlaveLostEvent(EntityUid slave) : base(slave)
    {
    }
}
