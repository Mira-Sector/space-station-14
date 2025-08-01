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

    public override void NodeReached(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.NodeReached(body, limb, user, used, bodyPart, out ui, out bodyUi);

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

    public override string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-special-change-sex-name");
    }

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        if (ForcedSex is { } sex)
            return Loc.GetString("surgery-special-change-sex-desc", ("sex", sex));
        else
            return Loc.GetString("surgery-special-change-sex-swap-desc");
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Icon;
    }
}
