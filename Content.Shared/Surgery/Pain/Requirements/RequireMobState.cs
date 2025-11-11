using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Pain.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class RequireMobState : SurgeryPainRequirement
{
    [DataField]
    public HashSet<MobState> AllowedStates = [];

    public override bool RequirementMet(IEntityManager entity, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        if (body == null)
            return false;

        if (!entity.TryGetComponent<MobStateComponent>(body.Value, out var mobState))
            return true;

        return AllowedStates.Contains(mobState.CurrentState);
    }
}
