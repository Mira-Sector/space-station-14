using System.Numerics;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared.Weapons.Ranged.Events;

[ByRefEvent]
public record struct HitScanShooterHitAttemptEvent(EntityUid Target, EntityUid SourceItem, ReflectType Reflective, Vector2 Direction, bool Cancelled = false);

/// <summary>
/// Shot may be reflected by setting <see cref="Reflected"/> to true
/// and changing <see cref="Direction"/> where shot will go next
/// </summary>
[ByRefEvent]
public record struct HitScanReflectAttemptEvent(EntityUid? Shooter, EntityUid SourceItem, ReflectType Reflective, Vector2 Direction, bool Reflected);
