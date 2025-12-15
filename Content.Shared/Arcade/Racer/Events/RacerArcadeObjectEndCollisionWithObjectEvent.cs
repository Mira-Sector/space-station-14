namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public sealed partial class RacerArcadeObjectEndCollisionWithObjectEvent(EntityUid other, string ourShapeId, RacerArcadeCollisionShapeEntry ourShape, string otherShapeId, RacerArcadeCollisionShapeEntry otherShape) : BaseRacerArcadeObjectCollisionWithObjectEvent(other, ourShapeId, ourShape, otherShapeId, otherShape);
