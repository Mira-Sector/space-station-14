namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public record struct RacerArcadeObjectPhysicsGetVelocityEvent(Vector3 Velocity, Vector3 AngularVelocity);
