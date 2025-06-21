using System.Linq;
using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class BodyDamageThresholdsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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

        if (ent.Comp.CurrentState == potentialState.Key)
            return;

        var ev = new BodyDamageThresholdChangedEvent(ent.Comp.CurrentState, potentialState.Key);
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.CurrentState = potentialState.Key;
        Dirty(ent);

        _appearance.SetData(ent.Owner, BodyDamageThresholdVisuals.State, potentialState.Key);
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

    [PublicAPI]
    public FixedPoint2 RelativeToState(Entity<BodyDamageThresholdsComponent?, BodyDamageableComponent?> ent, BodyDamageState state)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return FixedPoint2.Zero;

        if (ent.Comp1.CurrentState == state)
            return FixedPoint2.Zero;

        if (!ent.Comp1.Thresholds.TryGetValue(state, out var threshold))
            return FixedPoint2.Zero;

        return ent.Comp2.Damage - threshold;
    }
}
