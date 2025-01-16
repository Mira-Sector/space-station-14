using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.ClosetSkeleton;

[RegisterComponent]
public sealed partial class ClosetSkeletonComponent : Component
{
    [DataField]
    public ProtoId<StartingGearPrototype>? FallbackEquipment;
}
