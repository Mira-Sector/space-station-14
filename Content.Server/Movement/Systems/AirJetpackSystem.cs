using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Movement.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.Movement.Systems;

public sealed partial class AirJetpackSystem : EntitySystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirJetpackComponent, EnableJetpackAttemptEvent>(OnEnableAttempt);
    }

    private void OnEnableAttempt(Entity<AirJetpackComponent> ent, ref EnableJetpackAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<GasTankComponent>(ent.Owner, out var gasTank) || gasTank.Air.TotalMoles < ent.Comp.MoleUsage)
            args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDisable = new ValueList<(EntityUid Uid, JetpackComponent Component)>();
        var query = EntityQueryEnumerator<ActiveJetpackComponent, JetpackComponent, AirJetpackComponent, GasTankComponent>();

        while (query.MoveNext(out var uid, out var active, out var jetpack, out var comp, out var gasTankComp))
        {
            if (_timing.CurTime < active.TargetTime)
                continue;

            var gasTank = (uid, gasTankComp);
            active.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(active.EffectCooldown);
            var usedAir = _gasTank.RemoveAir(gasTank, comp.MoleUsage);

            if (usedAir == null)
                continue;

            var usedEnoughAir =
                MathHelper.CloseTo(usedAir.TotalMoles, comp.MoleUsage, comp.MoleUsage / 100);

            if (!usedEnoughAir)
                toDisable.Add((uid, jetpack));

            _gasTank.UpdateUserInterface(gasTank);
        }

        foreach (var (uid, comp) in toDisable)
            _jetpack.SetEnabled(uid, comp, false);
    }
}
