using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class AddLimb : SurgerySpecial
{
    public override void NodeReached(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var bodySys = entMan.System<SharedBodySystem>();

        foreach (var container in bodySys.GetBodyContainers(body))
        {
        }
    }

    public override void NodeLeft(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
    }
}

