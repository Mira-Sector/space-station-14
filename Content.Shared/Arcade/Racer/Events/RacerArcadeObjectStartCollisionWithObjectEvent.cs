namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public sealed partial class RacerArcadeObjectStartCollisionWithObjectEvent(EntityUid other, string ourShapeId, RacerArcadeCollisionShapeEntry ourShape, string otherShapeId, RacerArcadeCollisionShapeEntry otherShape) : BaseRacerArcadeObjectCollisionWithObjectEvent(other, ourShapeId, ourShape, otherShapeId, otherShape);
