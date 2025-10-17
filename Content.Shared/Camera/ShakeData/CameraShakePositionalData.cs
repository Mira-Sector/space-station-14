using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera.ShakeData;

[Serializable, NetSerializable]
public struct CameraShakePositionalData : ICameraShakeData
{
    [ViewVariables]
    public EntityCoordinates Position;

    public readonly bool TryGetDirection(Entity<CameraShakeComponent> ent, IEntityManager entity, out Vector2 direction)
    {
        var entXform = entity.GetComponent<TransformComponent>(ent.Owner);
        var xform = entity.System<SharedTransformSystem>();
        return Position.TryDelta(entity, xform, entXform.Coordinates, out direction);
    }
}
