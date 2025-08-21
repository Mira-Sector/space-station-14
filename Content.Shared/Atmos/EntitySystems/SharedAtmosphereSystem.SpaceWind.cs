using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected void UpdateSpaceWindMovableEntity(Entity<MovedByPressureComponent, TransformComponent, PhysicsComponent> ent, float frameTime)
    {
        if (!IsMovableByWind(ent!))
            return;

        var pushForce = ent.Comp1.CurrentWind * frameTime;

        if (pushForce.LengthSquared() < MovedByPressureComponent.MinPushForceSquared)
            return;

        if (ent.Comp1.StunForceThreshold is { } stunForceThreshold)
        {
            var stunForceDelta = stunForceThreshold * stunForceThreshold - pushForce.LengthSquared();

            if (stunForceDelta > 0)
                _stun.TryKnockdown(ent.Owner, ent.Comp1.StunDurationPerForce * stunForceDelta, true);
        }

        _physics.ApplyLinearImpulse(ent.Owner, pushForce * ent.Comp3.Mass, body: ent.Comp3);

        var ev = new SpaceWindMovedEvent(pushForce);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public bool IsMovableByWind(Entity<MovedByPressureComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.Enabled;
    }

    public void SetMovableByWind(Entity<MovedByPressureComponent?> ent, bool enable)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Enabled == enable)
            return;

        ent.Comp.Enabled = enable;
        Dirty(ent);
    }
}
