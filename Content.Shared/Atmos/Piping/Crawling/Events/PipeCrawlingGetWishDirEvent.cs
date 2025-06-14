using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Crawling.Events;

[Serializable, NetSerializable]
public sealed partial class PipeCrawlingGetWishDirEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public PipeCrawlingGetWishDirEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}
