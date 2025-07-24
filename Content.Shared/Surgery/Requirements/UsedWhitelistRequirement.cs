using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class UsedWhitelistRequirement : SurgeryEdgeRequirement
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? BlackList;

    [DataField]
    public TimeSpan? Delay;

    public override string Description(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return string.Empty;
    }

    public override SpriteSpecifier? GetIcon(EntityUid? body, EntityUid? limb, BodyPart bodyPart)
    {
        return null;
    }

    public override SurgeryEdgeState RequirementMet(EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? tool, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        if (tool == null)
            return SurgeryEdgeState.Failed;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var whitelistSystem = entMan.System<EntityWhitelistSystem>();

        if (whitelistSystem.IsWhitelistFailOrNull(Whitelist, tool.Value) || whitelistSystem.IsBlacklistFail(BlackList, tool.Value))
            return SurgeryEdgeState.Failed;

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

        if (other is not UsedWhitelistRequirement otherWhitelist)
            return false;

        if (Whitelist != otherWhitelist.Whitelist || BlackList != otherWhitelist.BlackList)
            return false;

        var delay = Delay ?? otherWhitelist.Delay;

        if (Delay != null && otherWhitelist.Delay != null)
            delay = TimeSpan.FromSeconds(Math.Min(Delay.Value.TotalSeconds, otherWhitelist.Delay.Value.TotalSeconds)); //use the shortest delay

        merged = new UsedWhitelistRequirement()
        {
            Whitelist = Whitelist,
            BlackList = BlackList,
            Delay = delay
        };

# if DEBUG
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;

        if (Delay != otherWhitelist.Delay)
            log.Warning($"Surgery UsedWhitelistRequirement has mismatching delays of {Delay} and {otherWhitelist.Delay} with {Whitelist}, {BlackList}.");
# endif

        return true;
    }
}
