using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Gets ammo for this NPC's selected gun; either active hand or itself.
/// </summary>
public sealed partial class GunAmmoPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("minPercent")]
    public float MinPercent = 0f;

    [DataField("maxPercent")]
    public float MaxPercent = 1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gunSystem = _entManager.System<GunSystem>();

        if (!gunSystem.TryGetGuns(owner, out var guns))
        {
            return false;
        }

        foreach (var (gunUid, _) in guns)
        {
            var ammoEv = new GetAmmoCountEvent();
            _entManager.EventBus.RaiseLocalEvent(gunUid, ref ammoEv);
            float percent;

            if (ammoEv.Capacity == 0)
                percent = 0f;
            else
                percent = ammoEv.Count / (float) ammoEv.Capacity;

            percent = System.Math.Clamp(percent, 0f, 1f);

            if (MaxPercent < percent)
                continue;

            if (MinPercent > percent)
                continue;

            return true;
        }

        return false;
    }
}
