using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.ClosetSkeleton;

[RegisterComponent]
public sealed partial class ClosetSkeletonLockerComponent : Component
{
    [DataField]
    public ProtoId<StartingGearPrototype> Equipment;
}
