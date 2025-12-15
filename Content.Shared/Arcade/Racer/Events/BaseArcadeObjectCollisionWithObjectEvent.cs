namespace Content.Shared.Arcade.Racer.Events;

public abstract partial class BaseRacerArcadeObjectCollisionWithObjectEvent(EntityUid other, string ourShapeId, RacerArcadeCollisionShapeEntry ourShape, string otherShapeId, RacerArcadeCollisionShapeEntry otherShape)
{
    public readonly EntityUid Other = other;
    public readonly string OurShapeId = ourShapeId;
    public readonly RacerArcadeCollisionShapeEntry OurShape = ourShape;
    public readonly string OtherShapeId = otherShapeId;
    public readonly RacerArcadeCollisionShapeEntry OtherShape = otherShape;
}
