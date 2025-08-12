using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;
using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class ChangeSex : SurgerySpecial
{
    [DataField]
    public Sex? ForcedSex;

    private static readonly SpriteSpecifier.Rsi Icon = new(new("/Textures/Interface/surgery_icons.rsi"), "sex");

    public override void NodeReached(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.NodeReached(receiver, body, limb, user, used, bodyPart, out ui, out bodyUi);

        if (body == null)
            return;

        var entity = IoCManager.Resolve<IEntityManager>();

        if (!entity.TryGetComponent<HumanoidAppearanceComponent>(body.Value, out var humanoidAppearance))
            return;

        var humanoid = entity.System<SharedHumanoidAppearanceSystem>();

        if (ForcedSex is not { } sex)
        {
            sex = humanoidAppearance.Sex switch
            {
                Sex.Male => Sex.Female,
                Sex.Female => Sex.Male,
                _ => Sex.Unsexed,
            };
        }

        humanoid.SetSex(body.Value, sex, true, humanoidAppearance);
        return;
    }

    public override bool Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out string? name)
    {
        name = Loc.GetString("surgery-special-change-sex-name");
        return true;
    }

    public override bool Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out string? description)
    {
        if (ForcedSex is { } sex)
            description = Loc.GetString("surgery-special-change-sex-desc", ("sex", sex));
        else
            description = Loc.GetString("surgery-special-change-sex-swap-desc");

        return true;
    }

    public override bool GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out SpriteSpecifier? icon)
    {
        icon = Icon;
        return true;
    }
}
