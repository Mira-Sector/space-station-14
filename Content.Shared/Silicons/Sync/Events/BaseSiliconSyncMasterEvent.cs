namespace Content.Shared.Silicons.Sync.Events;

public abstract partial class BaseSiliconSyncMasterEvent : EntityEventArgs
{
    public readonly EntityUid Slave;

    public BaseSiliconSyncMasterEvent(EntityUid slave)
    {
        Slave = slave;
    }
}
