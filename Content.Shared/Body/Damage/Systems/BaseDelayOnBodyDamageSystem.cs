using Content.Shared.Body.Damage.Components;
using Content.Shared.FixedPoint;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Body.Damage.Systems;

public abstract partial class BaseDelayOnBodyDamageSystem<T> : BaseOnBodyDamageSystem<T> where T : BaseDelayOnBodyDamageComponent
{
    [Dependency] protected readonly BodyDamageThresholdsSystem BodyDamageThresholds = default!;

    protected bool TryGetAdditionalDelay(Entity<T> ent, TimeSpan totalDelay, TimeSpan sourceDelay, [NotNullWhen(true)] out TimeSpan? delay)
    {
        delay = null;
        if (totalDelay > ent.Comp.MaxDelay)
            return false;

        if (!TryComp<BodyDamageThresholdsComponent>(ent.Owner, out var thresholdsComp))
            return false;

        if (!thresholdsComp.Thresholds.ContainsKey(ent.Comp.TargetState))
            return false;

        var maxAdditionalDelay = ent.Comp.MaxDelay - sourceDelay;

        if (thresholdsComp.CurrentState >= ent.Comp.TargetState)
        {
            delay = maxAdditionalDelay;
            return true;
        }

        var distanceToThreshold = FixedPoint2.Abs(BodyDamageThresholds.RelativeToState((ent.Owner, thresholdsComp), ent.Comp.TargetState));
        if (distanceToThreshold == FixedPoint2.Zero)
        {
            delay = maxAdditionalDelay;
            return true;
        }

        var delayScalingFactor = MathF.Min(1, 1 / (float)distanceToThreshold);
        delay = maxAdditionalDelay * delayScalingFactor;
        return true;
    }
}
