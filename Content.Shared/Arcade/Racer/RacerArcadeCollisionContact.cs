namespace Content.Shared.Arcade.Racer;

public readonly record struct RacerArcadeCollisionContact(
    string AId,
    RacerArcadeCollisionShapeEntry AEntry,
    string BId,
    RacerArcadeCollisionShapeEntry BEntry,
    Vector3 Normal,
    float Penetration
)
{
    public override string ToString()
    {
        return $"AId: {AId} AEntry: ( {AEntry} ) BId: {BId} BEntry: ( {BEntry} ) Normal: {Normal} Penetration: {Penetration}";
    }
}
