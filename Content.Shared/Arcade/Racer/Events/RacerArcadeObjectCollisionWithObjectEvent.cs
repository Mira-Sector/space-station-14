namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public record struct RacerArcadeObjectCollisionWithObjectEvent(EntityUid Other, string OurShapeId, string OtherShapeId);
