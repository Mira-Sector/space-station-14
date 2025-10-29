using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Pain.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class Sleeping : SurgeryPainRequirement
{
    [DataField]
    public bool AllowNonForced;

    public override bool RequirementMet(IEntityManager entity, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        if (body == null)
            return false;

        if (!entity.HasComponent<SleepingComponent>(body.Value))
            return true;

        if (AllowNonForced)
            return false;

        var statusEfffectSys = entity.System<SharedStatusEffectsSystem>();
        return !statusEfffectSys.HasEffectComp<ForcedSleepingStatusEffectComponent>(body.Value);
    }
}
