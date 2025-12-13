namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public record struct RacerArcadeObjectPhysicsGetForcesEvent(Vector3 Force, Vector3 Torque);
