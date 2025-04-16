using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed partial class SiliconSlaveRadialBoundUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, SpriteSpecifier?> Masters = new();
}
