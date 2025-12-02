namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public record struct RacerArcadeObjectCollisionWithTrackEvent(string OurShapeId);
