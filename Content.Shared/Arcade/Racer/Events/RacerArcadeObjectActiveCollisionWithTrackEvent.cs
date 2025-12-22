namespace Content.Shared.Arcade.Racer.Events;

[ByRefEvent]
public sealed partial class RacerArcadeObjectActiveCollisionWithTrackEvent(
    string ourShapeId,
    RacerArcadeCollisionShapeEntry ourShape,
    RacerArcadeCollisionShapeEntry otherShape,
    Vector3 normal,
    float penetration)
    : BaseRacerArcadeObjectCollisionWithTrackEvent(
        ourShapeId,
        ourShape,
        otherShape,
        normal,
        penetration);
