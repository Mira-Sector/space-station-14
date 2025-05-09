using Content.Shared.Stunnable;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class TargetStunnedPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField]
    public bool Stunned = true;

    [DataField(required: true)]
    public string TargetKey = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return false;

        var hasComp = _entManager.HasComponent<StunnedComponent>(target);

        if (Stunned)
            return hasComp;
        else
            return !hasComp;
    }
}
