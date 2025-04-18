using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

[Serializable, NetSerializable]
public sealed partial class SiliconGetLawsEvent : EntityEventArgs
{
    public readonly NetEntity Target;
    public SiliconLawset? Laws;

    public SiliconGetLawsEvent(NetEntity target)
    {
        Target = target;
    }
}
