using Content.Shared.Body.Organ;
using Robust.Shared.Timing;

namespace Content.Shared.Coughing;

public abstract partial class SharedCoughingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public virtual bool TryCough(Entity<CougherComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.LastCough + ent.Comp.MinCoughDelay > _timing.CurTime)
            return false;

        ent.Comp.LastCough = _timing.CurTime;
        return true;
    }

    public bool TryCoughBody(Entity<OrganComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Body is not {} body)
            return false;

        return TryCough(body);
    }
}
