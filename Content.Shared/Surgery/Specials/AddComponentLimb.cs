using Content.Shared.Body.Part;
using Content.Shared.Surgery.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class AddComponentLimb : SurgerySpecial
{
    [DataField(required: true)]
    [NonSerialized]
    public ComponentRegistry Components = [];

    [DataField("name", required: true)]
    public LocId NameLoc;

    [DataField("description", required: true)]
    public LocId DescLoc;

    [DataField(required: true)]
    public SpriteSpecifier Icon;

    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.NodeReached(body, limb, user, used, bodyPart, out ui, out bodyUi);

        if (limb == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(limb.Value, Components);
    }

    public override string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString(NameLoc, ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
    }

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString(DescLoc, ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Icon;
    }
}
