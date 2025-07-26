using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Surgery.Events;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class ToolRequirement : SurgeryEdgeRequirement
{
    [DataField(required: true)]
    public ProtoId<ToolQualityPrototype> Quality;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1f);

    public override string Name(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return Loc.GetString("surgery-requirement-tool-name");
    }

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();

        var qualityName = Loc.GetString(prototypes.Index(Quality).Name);

        return Loc.GetString("surgery-requirement-tool-desc", ("tool", qualityName));
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();

        return prototypes.Index(Quality).Icon;
    }

    public override SurgeryInteractionState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        if (tool == null)
            return SurgeryInteractionState.Failed;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        if (toolSystem.HasQuality(tool.Value, Quality))
            return Delay > TimeSpan.Zero ? SurgeryInteractionState.DoAfter : SurgeryInteractionState.Passed;

        return SurgeryInteractionState.Failed;
    }

    public override bool StartDoAfter(SharedDoAfterSystem doAfter, SurgeryEdge targetEdge, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, [NotNullWhen(true)] out DoAfterId? doAfterId)
    {
        doAfterId = null;

        if (tool == null)
            return false;

        if ((body ?? limb) == null)
            return false;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        return toolSystem.UseTool(tool.Value, user, limb ?? body, Delay, [Quality], new SurgeryEdgeRequirementDoAfterEvent(targetEdge, bodyPart), out doAfterId, requireDown: body != null ? true : null);
    }

    public override bool RequirementsMatch(SurgeryEdgeRequirement other, [NotNullWhen(true)] out SurgeryEdgeRequirement? merged)
    {
        merged = null;

        if (other is not ToolRequirement otherTool)
            return false;

        if (Quality != otherTool.Quality)
            return false;

        merged = new ToolRequirement()
        {
            Quality = Quality,
            Delay = TimeSpan.FromSeconds(Math.Min(Delay.TotalSeconds, otherTool.Delay.TotalSeconds)) //use the shortest delay
        };

# if DEBUG
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;

        if (Delay != otherTool.Delay)
            log.Warning($"Surgery ToolRequirement has mismatching delays of {Delay} and {otherTool.Delay} with {Quality}.");
# endif

        return true;
    }
}
