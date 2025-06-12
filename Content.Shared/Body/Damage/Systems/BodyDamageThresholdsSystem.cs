using System.Linq;
using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class BodyDamageThresholdsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyDamageThresholdsComponent, BodyDamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BodyDamageThresholdsComponent, BodyDamageCanDamageEvent>(OnCanDamage);
    }

    private void OnDamageChanged(Entity<BodyDamageThresholdsComponent> ent, ref BodyDamageChangedEvent args)
    {
        var index = ent.Comp.Thresholds.IndexOf(ent.Comp.CurrentState);
        var positive = args.NewDamage - args.OldDamage > 0;
        var potentialIndex = positive ? index + 1 : index - 1;

        if (potentialIndex > ent.Comp.Thresholds.Count || potentialIndex < 0)
            return;

        KeyValuePair<BodyDamageState, FixedPoint2> potentialState;
        potentialState = ent.Comp.Thresholds.GetAt(potentialIndex);

        if (positive)
        {
            if (potentialState.Value > args.NewDamage)
                return;
        }
        else
        {
            if (potentialState.Value < args.NewDamage)
                return;
        }

        ent.Comp.CurrentState = potentialState.Key;
        Dirty(ent);
    }

    private void OnCanDamage(Entity<BodyDamageThresholdsComponent> ent, ref BodyDamageCanDamageEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.PreventFurtherDamage)
            return;

        if (ent.Comp.Thresholds.Last().Key == ent.Comp.CurrentState)
            args.Cancel();
    }
}
