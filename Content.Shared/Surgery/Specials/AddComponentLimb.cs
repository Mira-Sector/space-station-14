using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly]
public sealed partial class AddComponentLimb : SurgerySpecial
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public override void NodeReached(EntityUid body, EntityUid limb)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(limb, Components);
    }
}
