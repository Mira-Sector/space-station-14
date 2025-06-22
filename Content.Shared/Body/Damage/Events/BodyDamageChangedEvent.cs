using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Damage.Events;

[ByRefEvent]
public record struct BodyDamageChangedEvent(FixedPoint2 OldDamage, FixedPoint2 NewDamage)
{
    public readonly FixedPoint2 OldDamage = OldDamage;
    public FixedPoint2 NewDamage = NewDamage;
}
