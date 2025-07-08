using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Examine;
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
        SubscribeLocalEvent<BodyDamageThresholdsComponent, ExaminedEvent>(OnExamine);
    }

    private void OnDamageChanged(Entity<BodyDamageThresholdsComponent> ent, ref BodyDamageChangedEvent args)
    {
        var index = (byte)ent.Comp.CurrentState;
        var positive = args.NewDamage - args.OldDamage > 0;
        var potentialIndex = positive ? ++index : --index;

        if (!Enum.IsDefined(typeof(BodyDamageState), potentialIndex))
            return;

        var potentialState = (BodyDamageState)potentialIndex;
        var potentialThreshold = ent.Comp.Thresholds[potentialState];

        if (positive)
        {
            if (potentialThreshold > args.NewDamage)
                return;
        }
        else
        {
            if (potentialThreshold < args.NewDamage)
                return;
        }

        if (ent.Comp.CurrentState == potentialState)
            return;

        var ev = new BodyDamageThresholdChangedEvent(ent.Comp.CurrentState, potentialState);
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.CurrentState = potentialState;
        Dirty(ent);

        _appearance.SetData(ent.Owner, BodyDamageThresholdVisuals.State, potentialState);

        if (!ent.Comp.PreventFurtherDamage)
            return;

        var (lastState, lastThreshold) = ent.Comp.Thresholds.Last();
        if (lastState != potentialState)
            return;

        if (args.NewDamage > lastThreshold)
            args.NewDamage = lastThreshold;
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

    private void OnExamine(Entity<BodyDamageThresholdsComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ExamineText.TryGetValue(ent.Comp.CurrentState, out var examine))
            return;

        args.PushMarkup(Loc.GetString(examine));
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

    [PublicAPI]
    public bool TryGetThreshold(Entity<BodyDamageThresholdsComponent?> ent, BodyDamageState state, out FixedPoint2 threshold)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
        {
            threshold = FixedPoint2.Zero;
            return false;
        }

        return ent.Comp.Thresholds.TryGetValue(state, out threshold);
    }
}
