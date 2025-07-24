using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
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
    private static readonly ResPath LimbIcons = new("/Textures/Interface/Alerts/limb_damage.rsi");

    [DataField]
    public TimeSpan? Delay;

    [DataField]
    public bool RequireAiming = true;

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-requirement-body-part-desc", ("part", Loc.GetString(SurgeryHelper.GetBodyPartLoc(bodyPart))));
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        var state = SharedBodySystem.BodyPartToLayer(bodyPart);
        return new SpriteSpecifier.Rsi(LimbIcons, state.ToString());
    }

    public override SurgeryEdgeState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        if (tool == null)
            return SurgeryEdgeState.Failed;

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryGetComponent<BodyPartComponent>(tool.Value, out var bodyPartComp))
            return SurgeryEdgeState.Failed;

        if (bodyPart != null)
        {
            if (bodyPartComp.PartType != bodyPart.Type || bodyPartComp.Symmetry != bodyPart.Side)
                return SurgeryEdgeState.Failed;
        }

        if (RequireAiming)
        {
            if (!entMan.TryGetComponent<DamagePartSelectorComponent>(user, out var damageSelectorComp))
                return SurgeryEdgeState.Failed;

            if (bodyPart != null)
            {
                if (damageSelectorComp.SelectedPart.Type != bodyPart.Type || damageSelectorComp.SelectedPart.Side != bodyPart.Side)
                    return SurgeryEdgeState.Failed;
            }
            else
            {
                if (damageSelectorComp.SelectedPart.Type != bodyPartComp.PartType || damageSelectorComp.SelectedPart.Side != bodyPartComp.Symmetry)
                    return SurgeryEdgeState.Failed;
            }
        }

        if (Delay == null)
            return SurgeryEdgeState.Passed;

        return SurgeryEdgeState.DoAfter;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        if (tool == null || Delay == null)
            return false;

        var entMan = IoCManager.Resolve<IEntityManager>();

        var doAfterArgs = new DoAfterArgs(entMan, user, Delay.Value, new SurgeryDoAfterEvent(targetEdge, bodyPart), limb, used: tool)
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
