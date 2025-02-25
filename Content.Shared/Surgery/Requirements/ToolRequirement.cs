using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class ToolRequirement : SurgeryEdgeRequirement
{
    [DataField]
    public List<string> Qualities = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1f);

    public override SurgeryEdgeState RequirementMet(EntityUid body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        if (tool == null)
            return SurgeryEdgeState.Failed;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        if (toolSystem.HasAllQualities(tool.Value, Qualities))
            return SurgeryEdgeState.DoAfter;

        return SurgeryEdgeState.Failed;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        if (tool == null)
            return false;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        return toolSystem.UseTool(tool.Value, user, limb ?? body, Delay, Qualities, new SurgeryDoAfterEvent(targetEdge, bodyPart), out doAfterId, requireDown: true);
    }

    public override bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged)
    {
        merged = null;

        if (other is not ToolRequirement otherTool)
            return false;

        if (Qualities.Count != otherTool.Qualities.Count)
            return false;

        foreach (var quality in Qualities)
        {
            var matchFound = false;

            foreach (var otherQuality in otherTool.Qualities)
            {
                if (quality != otherQuality)
                    continue;

                matchFound = true;
                break;
            }

            if (!matchFound)
                return false;
        }

        merged = new ToolRequirement()
        {
            Qualities = Qualities,
            Delay = TimeSpan.FromSeconds(Math.Min(Delay.TotalSeconds, otherTool.Delay.TotalSeconds)) //use the shortest delay
        };

# if DEBUG
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;

        if (Delay != otherTool.Delay)
            log.Warning($"Surgery ToolRequirement has mismatching delays of {Delay} and {otherTool.Delay} with {Qualities}.");
# endif

        return true;
    }
}
