using System.Diagnostics.CodeAnalysis;
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

    [DataField("name")]
    public LocId? NameLoc;

    [DataField("description")]
    public LocId? DescLoc;

    [DataField]
    public SpriteSpecifier? Icon;

    public override void NodeReached(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart? bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.NodeReached(receiver, body, limb, user, used, bodyPart, out ui, out bodyUi);

        if (limb == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(limb.Value, Components);
    }

    public override bool Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart, [NotNullWhen(true)] out string? name)
    {
        if (NameLoc == null)
        {
            name = null;
            return false;
        }
        else
        {
            name = Loc.GetString(NameLoc, ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
            return true;
        }
    }

    public override bool Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart, [NotNullWhen(true)] out string? description)
    {
        if (DescLoc == null)
        {
            description = null;
            return false;
        }
        else
        {
            description = Loc.GetString(DescLoc, ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
            return true;
        }
    }

    public override bool GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart, [NotNullWhen(true)] out SpriteSpecifier? icon)
    {
        if (Icon == null)
        {
            icon = null;
            return false;
        }
        else
        {
            icon = Icon;
            return true;
        }
    }
}
