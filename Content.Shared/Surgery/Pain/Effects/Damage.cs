using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Pain.Effects;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class Damage : SurgeryPainEffect
{
    [DataField("damage", required: true)]
    public DamageSpecifier DamageSpecifier;

    public override void DoEffect(IEntityManager entity, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        var damageSys = entity.System<DamageableSystem>();
        damageSys.TryChangeDamage(limb ?? body, DamageSpecifier);
    }
}
