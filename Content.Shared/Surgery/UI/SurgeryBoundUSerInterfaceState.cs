using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.UI;

[Serializable, NetSerializable]
public sealed partial class SurgeryBoundUserInterfaceState(NetEntity? target) : BoundUserInterfaceState
{
    public readonly NetEntity? Target = target;
}
