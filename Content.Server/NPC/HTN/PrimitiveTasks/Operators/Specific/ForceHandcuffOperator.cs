using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class ForceHandcuffOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private SharedCuffableSystem _cuffable = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _cuffable = sysManager.GetEntitySystem<SharedCuffableSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<CanForceHandcuffComponent>(owner, out var canForceCuff))
            return HTNOperatorStatus.Failed;

        return _cuffable.ForceCuff(canForceCuff, target, owner) ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
