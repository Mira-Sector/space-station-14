using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Surgery.Systems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class OrganManipulation : SurgerySpecial
{
    private static readonly ResPath RsiPath = new("/Textures/Interface/surgery_icons.rsi");

    public override SurgeryInteractionState Interacted(SurgerySpecialInteractionPhase phase, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart? bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.Interacted(phase, receiver, body, limb, user, used, bodyPart, out ui, out bodyUi);

        // affects surgery so do before all else
        if (phase != SurgerySpecialInteractionPhase.BeforeGraph)
            return SurgeryInteractionState.Failed;

        if (used != null)
            return SurgeryInteractionState.Failed;

        ui = OrganSelectionUiKey.Key;
        return SurgeryInteractionState.UserInterface;
    }

    public override bool Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart, [NotNullWhen(true)] out string? name)
    {
        name = Loc.GetString("surgery-special-organ-manipulation-name");
        return true;
    }

    public override bool Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart, [NotNullWhen(true)] out string? description)
    {
        description = Loc.GetString("surgery-special-organ-manipulation-desc", ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
        return true;
    }

    public override bool GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart, [NotNullWhen(true)] out SpriteSpecifier? icon)
    {
        icon = bodyPart?.Type switch
        {
            BodyPartType.Head => new SpriteSpecifier.Rsi(RsiPath, "organ-head"),
            BodyPartType.Torso => new SpriteSpecifier.Rsi(RsiPath, "organ-torso"),
            _ => throw new NotImplementedException()
        };

        return true;
    }
}
