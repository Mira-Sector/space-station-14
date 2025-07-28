using Content.Shared.Surgery.Pain;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    public bool TryDealPain(ProtoId<SurgeryPainPrototype>? painId, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        if (painId == null)
            return false;

        return TryDealPain(painId.Value, body, limb, used);
    }

    public bool TryDealPain(ProtoId<SurgeryPainPrototype> painId, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        if (!_prototype.TryIndex(painId, out var pain))
            return false;

        if (!PainRequirementsMet(pain, body, limb, used))
            return false;

        DoPainEffects(pain, body, limb, used);
        return true;
    }

    private bool PainRequirementsMet(SurgeryPainPrototype pain, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        foreach (var requirement in pain.Requirements)
        {
            if (!requirement.RequirementMet(EntityManager, body, limb, used))
                return false;
        }

        return true;
    }

    private void DoPainEffects(SurgeryPainPrototype pain, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        foreach (var effect in pain.Effects)
            effect.DoEffect(EntityManager, body, limb, used);
    }
}
