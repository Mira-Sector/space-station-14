using Content.Shared.Body.Damage.Components;

namespace Content.Shared.Body.Damage.Events;

[ByRefEvent]
public readonly record struct BodyDamageThresholdChangedEvent(BodyDamageState OldState, BodyDamageState NewState);
