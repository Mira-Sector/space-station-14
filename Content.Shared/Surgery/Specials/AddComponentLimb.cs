using Content.Shared.Body.Part;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class AddComponentLimb : SurgerySpecial
{
    [DataField(required: true)]
    [NonSerialized]
    public ComponentRegistry Components = [];

    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        base.NodeReached(body, limb, user, used, bodyPart, out ui);

        if (limb == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(limb.Value, Components);
    }
}
