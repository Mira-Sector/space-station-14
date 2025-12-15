namespace Content.Shared.Arcade.Racer.Events;

public abstract partial class BaseRacerArcadeObjectCollisionWithTrackEvent(string ourShapeId, RacerArcadeCollisionShapeEntry ourShape, RacerArcadeCollisionShapeEntry otherShape)
{
    public readonly string OurShapeId = ourShapeId;
    public readonly RacerArcadeCollisionShapeEntry OurShape = ourShape;
    public readonly RacerArcadeCollisionShapeEntry OtherShape = otherShape;
}
