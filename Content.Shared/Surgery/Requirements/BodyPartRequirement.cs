using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Surgery.Events;
using Content.Shared.Surgery.Systems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class BodyPartRequirement : SurgeryEdgeRequirement
{
    private static readonly ResPath IconRsi = new("/Textures/Interface/Actions/zone_sel.rsi");

    [DataField]
    public TimeSpan? Delay;

    [DataField]
    public bool RequireAiming = true;

    public override string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-requirement-body-part-name");
    }

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-requirement-body-part-desc", ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return new SpriteSpecifier.Rsi(IconRsi, SurgeryHelper.BodyPartIconState(bodyPart));
    }

    public override SurgeryInteractionState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui, bool test = false)
    {
        ui = null;

        if (tool == null)
            return SurgeryInteractionState.Failed;

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryGetComponent<BodyPartComponent>(tool.Value, out var bodyPartComp))
            return SurgeryInteractionState.Failed;

        if (bodyPart != null)
        {
            if (bodyPartComp.PartType != bodyPart.Type || bodyPartComp.Symmetry != bodyPart.Side)
                return SurgeryInteractionState.Failed;
        }

        if (RequireAiming)
        {
            if (!entMan.TryGetComponent<DamagePartSelectorComponent>(user, out var damageSelectorComp))
                return SurgeryInteractionState.Failed;

            if (bodyPart != null)
            {
                if (damageSelectorComp.SelectedPart.Type != bodyPart.Type || damageSelectorComp.SelectedPart.Side != bodyPart.Side)
                    return SurgeryInteractionState.Failed;
            }
            else
            {
                if (damageSelectorComp.SelectedPart.Type != bodyPartComp.PartType || damageSelectorComp.SelectedPart.Side != bodyPartComp.Symmetry)
                    return SurgeryInteractionState.Failed;
            }
        }

        if (Delay == null)
            return SurgeryInteractionState.Passed;

        return SurgeryInteractionState.DoAfter;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        if (tool == null || Delay == null)
            return false;

        var entMan = IoCManager.Resolve<IEntityManager>();

        var doAfterArgs = new DoAfterArgs(entMan, user, Delay.Value, new SurgeryEdgeRequirementDoAfterEvent(targetEdge, bodyPart), limb, used: tool)
        {
            BreakOnMove = true,
            RequireDown = true
        };

        return doAfter.TryStartDoAfter(doAfterArgs, out doAfterId);
    }

    public override bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged)
    {
        merged = null;

        if (other is not BodyPartRequirement otherRequirement)
            return false;

        if (RequireAiming != otherRequirement.RequireAiming)
            return false;

        var delay = Delay ?? otherRequirement.Delay;

        if (Delay != null && otherRequirement.Delay != null)
            delay = TimeSpan.FromSeconds(Math.Min(Delay.Value.TotalSeconds, otherRequirement.Delay.Value.TotalSeconds)); //use the shortest delay

        merged = new BodyPartRequirement()
        {
            RequireAiming = RequireAiming,
            Delay = delay
        };

# if DEBUG
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;

        if (Delay != otherRequirement.Delay)
            log.Warning($"Surgery BodyPartRequirement has mismatching delays of {Delay} and {otherRequirement.Delay} with {RequireAiming}.");
# endif

        return true;
    }
}
