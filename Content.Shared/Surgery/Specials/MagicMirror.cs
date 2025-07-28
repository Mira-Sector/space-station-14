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

    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
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

    public override void NodeLeft(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        ui = null;
        bodyUi = false;
        if (body == null)
            return;

        var entity = IoCManager.Resolve<IEntityManager>();
        entity.RemoveComponent<MagicMirrorComponent>(body.Value);
    }

    public override SurgeryInteractionState Interacted(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.Interacted(body, limb, user, used, bodyPart, out ui, out bodyUi);

        if (used != null)
            return SurgeryInteractionState.Failed;

        bodyUi = true;
        ui = MagicMirrorUiKey.Key;
        return SurgeryInteractionState.UserInterface;
    }

    public override string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-special-magic-mirror-name");
    }

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-special-magic-mirror-desc");
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Icon;
    }
}
