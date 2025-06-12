using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Damage.Events;

[ByRefEvent]
public readonly record struct BodyDamageChangedEvent(FixedPoint2 OldDamage, FixedPoint2 NewDamage);
