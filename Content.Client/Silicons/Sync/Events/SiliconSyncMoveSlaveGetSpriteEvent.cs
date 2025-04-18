using Content.Shared.Silicons.Sync;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.Sync.Events;

public sealed partial class SiliconSyncMoveSlaveGetPathSpriteEvent : EntityEventArgs
{
    public SpriteSpecifier? Icon;
    public SiliconSyncCommandingPathType PathType;

    public SiliconSyncMoveSlaveGetPathSpriteEvent(SiliconSyncCommandingPathType pathType)
    {
        PathType = pathType;
    }
}
