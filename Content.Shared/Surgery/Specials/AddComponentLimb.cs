using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class AddComponentLimb : SurgerySpecial
{
    [DataField(required: true)]
    [NonSerialized]
    public ComponentRegistry Components = new();

    public override void NodeReached(EntityUid body, EntityUid limb)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(limb, Components);
    }

    public override void NodeLeft(EntityUid body, EntityUid limb)
    {
    }
}
