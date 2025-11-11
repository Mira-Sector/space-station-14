using Content.Shared.Surgery.Pain;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    public bool TryDealPain(IEnumerable<ProtoId<SurgeryPainPrototype>> painIds, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        var handled = false;
        foreach (var painId in painIds)
            handled |= TryDealPain(painId, receiver, body, limb, used);

        return handled;
    }

    public bool TryDealPain(ProtoId<SurgeryPainPrototype> painId, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        if (!_prototype.TryIndex(painId, out var pain))
            return false;

        if (!PainRequirementsMet(pain, receiver, body, limb, used))
            return false;

        DoPainEffects(pain, receiver, body, limb, used);
        return true;
    }

    private bool PainRequirementsMet(SurgeryPainPrototype pain, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        foreach (var requirement in pain.Requirements)
        {
            if (!requirement.RequirementMet(EntityManager, receiver, body, limb, used))
                return false;
        }

        return true;
    }

    private void DoPainEffects(SurgeryPainPrototype pain, EntityUid receiver, EntityUid? body, EntityUid? limb, EntityUid? used)
    {
        foreach (var effect in pain.Effects)
            effect.DoEffect(EntityManager, receiver, body, limb, used);
    }
}
