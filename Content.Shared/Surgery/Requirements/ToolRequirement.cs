using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Requirements;

[UsedImplicitly]
public sealed partial class ToolRequirement : SurgeryEdgeRequirement
{
    [DataField(required: true)]
    public ProtoId<ToolQualityPrototype> Tool;

    public override bool RequirementMet(EntityUid body, EntityUid limb, EntityUid user, EntityUid tool)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var toolSystem = entMan.System<SharedToolSystem>();

        return toolSystem.HasQuality(tool, Tool);
    }
}
