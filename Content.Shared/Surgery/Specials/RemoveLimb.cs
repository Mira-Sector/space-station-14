using Content.Shared.Body.Part;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class RemoveLimb : SurgerySpecial
{
    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        base.NodeReached(body, limb, user, used, bodyPart, out ui);

        if (limb == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var handSys = entMan.System<SharedHandsSystem>();

        handSys.PickupOrDrop(user, limb.Value);
    }
}
