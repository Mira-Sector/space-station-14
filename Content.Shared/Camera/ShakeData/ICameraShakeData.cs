using System.Numerics;

namespace Content.Shared.Camera.ShakeData;

public interface ICameraShakeData
{
    bool TryGetDirection(Entity<CameraShakeComponent> ent, IEntityManager entity, out Vector2 direction);
}
