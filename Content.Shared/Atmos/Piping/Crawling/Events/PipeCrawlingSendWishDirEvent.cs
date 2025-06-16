using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Crawling.Events;

[Serializable, NetSerializable]
public sealed partial class PipeCrawlingSendWishDirEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly Vector2 WishDir;

    public PipeCrawlingSendWishDirEvent(NetEntity netEntity, Vector2 wishDir)
    {
        NetEntity = netEntity;
        WishDir = wishDir;
    }
}
