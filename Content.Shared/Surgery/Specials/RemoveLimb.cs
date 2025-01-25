using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class RemoveLimb : SurgerySpecial
{
    public override void NodeReached(EntityUid body, EntityUid limb, EntityUid user, EntityUid? used)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var handSys = entMan.System<SharedHandsSystem>();

        handSys.PickupOrDrop(user, limb);
    }

    public override void NodeLeft(EntityUid body, EntityUid limb, EntityUid user, EntityUid? used)
    {
    }
}
