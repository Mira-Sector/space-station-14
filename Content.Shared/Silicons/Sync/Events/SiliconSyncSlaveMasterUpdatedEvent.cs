namespace Content.Shared.Silicons.Sync.Events;

public sealed partial class SiliconSyncSlaveMasterUpdatedEvent : EntityEventArgs
{
    public readonly EntityUid? Master;
    public readonly EntityUid? OldMaster;

    public SiliconSyncSlaveMasterUpdatedEvent(EntityUid? master, EntityUid? oldMaster)
    {
        Master = master;
        OldMaster = oldMaster;
    }
}
