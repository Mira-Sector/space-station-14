using Content.Shared.Atmos.Components;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    public bool IsMovableByWind(Entity<MovedByPressureComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return true;
    }

    public void SetMovableByWind(Entity<MovedByPressureComponent?> ent, bool enable)
    {
    }
}
