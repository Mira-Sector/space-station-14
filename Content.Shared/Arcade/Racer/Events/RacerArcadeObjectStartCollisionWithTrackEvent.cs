namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public sealed partial class RacerArcadeObjectStartCollisionWithTrackEvent(string ourShapeId, RacerArcadeCollisionShapeEntry ourShape, RacerArcadeCollisionShapeEntry otherShape) : BaseRacerArcadeObjectCollisionWithTrackEvent(ourShapeId, ourShape, otherShape);
