using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed class AppendixSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AppendixComponent, StartedRottingEvent>(OnAppendixRotting);
        SubscribeLocalEvent<AppendixComponent, ExaminedEvent>(OnAppendixExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AppendixComponent, OrganComponent>();

        while (query.MoveNext(out var uid, out var appendixComp, out var organComp))
        {
            if (!appendixComp.Burst)
                continue;

            if (organComp.Body is not {} body)
                continue;

            if (appendixComp.NextDamage > _timing.CurTime)
                continue;

            appendixComp.NextDamage += appendixComp.DamageDelay;
            _damageable.TryChangeDamageBody(body, appendixComp.BurstDamage, interruptsDoAfters: false);
        }
    }

    private void OnAppendixRotting(EntityUid uid, AppendixComponent component, StartedRottingEvent args)
    {
        component.Burst = true;
        component.NextDamage = _timing.CurTime;
    }

    private void OnAppendixExamined(EntityUid uid, AppendixComponent component, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(component.Burst ? component.BurstExamine : component.NotBurstExamine));
    }
}
