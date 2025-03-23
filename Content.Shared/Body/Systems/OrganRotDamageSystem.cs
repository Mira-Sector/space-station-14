using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed class OrganRotDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganRotDamageComponent, StartedRottingEvent>(OnRotting);
        SubscribeLocalEvent<OrganRotDamageComponent, RotUpdateEvent>(OnRotUpdate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OrganRotDamageComponent, OrganComponent>();

        while (query.MoveNext(out var uid, out var organRotComp, out var organComp))
        {
            if (organComp.Body is not {} body)
                continue;

            if (organRotComp.NextDamage > _timing.CurTime)
                return;

            organRotComp.NextDamage = _timing.CurTime + organRotComp.DamageDelay;

            if (_mobState.IsDead(body))
                continue;

            if (!organRotComp.Enabled)
                continue;

            _damageable.TryChangeDamageBody(body, organRotComp.Damage, interruptsDoAfters: false);
        }
    }

    private void OnRotting(EntityUid uid, OrganRotDamageComponent component, StartedRottingEvent args)
    {
        component.Enabled = true;
        component.Damage = component.MaxDamage;
    }

    private void OnRotUpdate(EntityUid uid, OrganRotDamageComponent component, RotUpdateEvent args)
    {
        if (args.Stage < component.MinStage)
            return;

        if (component.Mode != OrganRotDamageMode.Rotting)
            return;

        if (args.RotProgress <= 0f)
            return;

        component.Enabled = true;
        component.Damage = component.MaxDamage * args.RotProgress;
    }
}
