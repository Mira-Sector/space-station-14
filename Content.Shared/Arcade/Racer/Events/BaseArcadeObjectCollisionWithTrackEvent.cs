namespace Content.Shared.Arcade.Racer.Events;

public abstract partial class BaseRacerArcadeObjectCollisionWithTrackEvent(
    string ourShapeId,
    RacerArcadeCollisionShapeEntry ourShape,
    RacerArcadeCollisionShapeEntry otherShape,
    Vector3 normal,
    float penetration)
{
    public readonly string OurShapeId = ourShapeId;
    public readonly RacerArcadeCollisionShapeEntry OurShape = ourShape;
    public readonly RacerArcadeCollisionShapeEntry OtherShape = otherShape;
    public readonly Vector3 Normal = normal;
    public readonly float Penetration = penetration;
}
