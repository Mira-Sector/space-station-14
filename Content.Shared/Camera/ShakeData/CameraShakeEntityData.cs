using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.Camera.ShakeData;

[Serializable, NetSerializable]
public struct CameraShakeEntityData : ICameraShakeData
{
    [ViewVariables]
    public EntityUid Target;

    public readonly bool TryGetDirection(Entity<CameraShakeComponent> ent, IEntityManager entity, out Vector2 direction)
    {
        if (!entity.EntityExists(Target))
        {
            direction = Vector2.Zero;
            return false;
        }

        var xform = entity.System<SharedTransformSystem>();

        var targetXform = entity.GetComponent<TransformComponent>(Target);
        var entXform = entity.GetComponent<TransformComponent>(ent.Owner);

        return targetXform.Coordinates.TryDelta(entity, xform, entXform.Coordinates, out direction);
    }
}
