using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializeTurret()
    {
        SubscribeLocalEvent<StationAiTurretVisualsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StationAiTurretVisualsComponent, NpcRangeAttemptEvent>(OnAttemptShoot);
        SubscribeLocalEvent<StationAiTurretVisualsComponent, GunShotEvent>(OnShot);
        SubscribeLocalEvent<StationAiTurretVisualsComponent, NpcRangeTargetLostEvent>(OnTargetLost);
    }

    private void UpdateTurret(float frameTime)
    {
        var query = EntityQueryEnumerator<StationAiTurretVisualsComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CurrentState == TurretState.Closing)
            {
                if (component.LastUpdate + component.ClosingTime > _timing.CurTime)
                    continue;

                component.CurrentState = TurretState.Closed;
                component.LastUpdate = _timing.CurTime;
                Dirty(uid, component);

                _appearance.SetData(uid, TurretVisuals.State, TurretState.Closed);
            }
        }
    }

    private void OnInit(EntityUid uid, StationAiTurretVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<NPCRangedCombatComponent>(uid, out var npcCombat))
            return;

        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        component.OpeningTime = TimeSpan.FromSeconds(npcCombat.ShootDelay);
        component.ClosingTime = TimeSpan.FromSeconds(1f / gun.FireRate);

        component.CurrentState = TurretState.Closed;
        component.LastUpdate = _timing.CurTime;

        Dirty(uid, component);

        _appearance.SetData(uid, TurretVisuals.State, TurretState.Closed);
    }

    private void OnAttemptShoot(EntityUid uid, StationAiTurretVisualsComponent component, ref NpcRangeAttemptEvent args)
    {
        component.CurrentState = TurretState.Opening;
        component.LastUpdate = _timing.CurTime;
        Dirty(uid, component);
        _appearance.SetData(uid, TurretVisuals.State, TurretState.Opening);
    }

    private void OnShot(EntityUid uid, StationAiTurretVisualsComponent component, ref GunShotEvent args)
    {
        component.CurrentState = TurretState.Open;
        component.LastUpdate = _timing.CurTime;
        Dirty(uid, component);
        _appearance.SetData(uid, TurretVisuals.State, TurretState.Open);
    }

    private void OnTargetLost(EntityUid uid, StationAiTurretVisualsComponent component, ref NpcRangeTargetLostEvent args)
    {
        component.CurrentState = TurretState.Closing;
        component.LastUpdate = _timing.CurTime;
        Dirty(uid, component);
        _appearance.SetData(uid, TurretVisuals.State, TurretState.Closing);
    }
}
