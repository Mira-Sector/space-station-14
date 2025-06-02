using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class EmptyHandRequirement : SurgeryEdgeRequirement
{
    [DataField]
    public TimeSpan? Delay;

    public override SurgeryEdgeState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var handSys = entMan.System<SharedHandsSystem>();

        if (handSys.TryGetActiveItem(user, out _))
            return SurgeryEdgeState.Failed;

        if (Delay == null)
            return SurgeryEdgeState.Passed;

        return SurgeryEdgeState.DoAfter;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        if (Delay == null)
            return false;

        var entMan = IoCManager.Resolve<IEntityManager>();

        var doAfterArgs = new DoAfterArgs(entMan, user, Delay.Value, new SurgeryDoAfterEvent(targetEdge, bodyPart), limb, used: tool)
        {
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            RequireDown = true
        };

        return doAfter.TryStartDoAfter(doAfterArgs, out doAfterId);
    }

    public override bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged)
    {
        merged = null;

        if (other is not EmptyHandRequirement otherEmptyHand)
            return false;

        var delay = Delay ?? otherEmptyHand.Delay;

        if (Delay != null && otherEmptyHand.Delay != null)
            delay = TimeSpan.FromSeconds(Math.Min(Delay.Value.TotalSeconds, otherEmptyHand.Delay.Value.TotalSeconds)); //use the shortest delay

        merged = new EmptyHandRequirement()
        {
            Delay = delay
        };

# if DEBUG
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;

        if (Delay != otherEmptyHand.Delay)
            log.Warning($"Surgery EmptyHandRequirement has mismatching delays of {Delay} and {otherEmptyHand.Delay}.");
# endif

        return true;
    }
}
