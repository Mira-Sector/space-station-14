using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
public readonly record struct NetRacerArcadeCollisionContact(
    string AId,
    string BId,
    Vector3 Normal,
    float Penetration
)
{
    public override string ToString()
    {
        return $"AId: {AId} BId: {BId} Normal: {Normal} Penetration: {Penetration}";
    }
}
