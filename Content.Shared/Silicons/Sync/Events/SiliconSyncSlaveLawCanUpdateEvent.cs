using Content.Shared.Silicons.Laws;

namespace Content.Shared.Silicons.Sync.Events;

public sealed partial class SiliconSyncSlaveLawCanUpdateEvent : CancellableEntityEventArgs
{
    public EntityUid Master;
    public SiliconLawset Laws;

    public SiliconSyncSlaveLawCanUpdateEvent(EntityUid master, SiliconLawset laws)
    {
        Master = master;
        Laws = laws;
    }
}
