using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Damage.Systems;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Surgery.Specials;

[UsedImplicitly, Serializable, NetSerializable]
public sealed partial class BodyDamage : SurgerySpecial
{
    [DataField(required: true)]
    public FixedPoint2 Damage;

    private static readonly ResPath RsiPath = new("/Textures/Interface/surgery_icons.rsi");

    public override void NodeReached(EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui, out bool bodyUi)
    {
        base.NodeReached(receiver, body, limb, user, used, bodyPart, out ui, out bodyUi);

        var entity = IoCManager.Resolve<IEntityManager>();
        var bodyDamage = entity.System<BodyDamageableSystem>();
        bodyDamage.ChangeDamage(receiver, Damage);
        return;
    }

    public override bool Name(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out string? name)
    {
        name = Loc.GetString("surgery-special-body-damage-name", ("deltasign", FixedPoint2.Sign(Damage)));
        return true;
    }

    public override bool Description(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out string? description)
    {
        description = Loc.GetString("surgery-special-body-damage-desc", ("amount", MathF.Abs(Damage.Float())), ("deltasign", FixedPoint2.Sign(Damage)));
        return true;
    }

    public override bool GetIcon(EntityUid receiver, EntityUid? body, EntityUid? limb, BodyPart bodyPart, [NotNullWhen(true)] out SpriteSpecifier? icon)
    {
        if (Damage > 0)
            icon = new SpriteSpecifier.Rsi(RsiPath, "bodydamage-damage");
        else
            icon = new SpriteSpecifier.Rsi(RsiPath, "bodydamage-heal");

        return true;
    }
}
