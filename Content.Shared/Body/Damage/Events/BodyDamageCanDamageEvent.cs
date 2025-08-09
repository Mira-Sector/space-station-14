using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Damage.Events;

public sealed partial class BodyDamageCanDamageEvent(FixedPoint2 currentDamage, FixedPoint2 targetDamage) : CancellableEntityEventArgs
{
    public readonly FixedPoint2 CurrentDamage = currentDamage;
    public readonly FixedPoint2 TargetDamage = targetDamage;
}
