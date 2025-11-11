using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera.ShakeData;

[Serializable, NetSerializable]
public struct CameraShakeDirectionData : ICameraShakeData
{
    [ViewVariables]
    public Vector2 Direction;

    public readonly bool TryGetDirection(Entity<CameraShakeComponent> ent, IEntityManager entity, out Vector2 direction)
    {
        direction = Direction;
        return true;
    }
}
