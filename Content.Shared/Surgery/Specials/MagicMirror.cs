using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;
using Content.Shared.MagicMirror;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class MagicMirror : SurgerySpecial
{
    private static readonly SpriteSpecifier.Rsi Icon = new(new("/Textures/Objects/Tools/scissors.rsi"), "icon");

    public override void NodeReached(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;
        if (body == null)
            return;

        var entity = IoCManager.Resolve<IEntityManager>();
        entity.EnsureComponent<MagicMirrorComponent>(body.Value, out var magicMirror);
        magicMirror.ActivateOnInteract = false;
        magicMirror.Target = body.Value;
        entity.Dirty(body.Value, magicMirror);
    }

    public override void NodeLeft(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;
        if (body == null)
            return;

        var entity = IoCManager.Resolve<IEntityManager>();
        entity.RemoveComponent<MagicMirrorComponent>(body.Value);
    }

    public override SurgeryInteractionState Interacted(SurgerySpecialInteractionPhase phase, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.Interacted(phase, receiver, body, limb, user, used, bodyPart, out ui, out bodyUi);

        // not important so only after
        if (phase != SurgerySpecialInteractionPhase.AfterGraph)
            return SurgeryInteractionState.Failed;

        if (used != null)
            return SurgeryInteractionState.Failed;

        bodyUi = true;
        ui = MagicMirrorUiKey.Key;
        return SurgeryInteractionState.UserInterface;
    }

    public override bool Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out string? name)
    {
        name = Loc.GetString("surgery-special-magic-mirror-name");
        return true;
    }

    public override bool Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out string? description)
    {
        description = Loc.GetString("surgery-special-magic-mirror-desc");
        return true;
    }

    public override bool GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out SpriteSpecifier? icon)
    {
        icon = Icon;
        return true;
    }
}
