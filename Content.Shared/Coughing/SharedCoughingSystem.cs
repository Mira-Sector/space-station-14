using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Organ;
using Robust.Shared.Timing;

namespace Content.Shared.Coughing;

public abstract partial class SharedCoughingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoughRotChangeModifyComponent, CoughGetChangceEvent>(OnRotGetChance);
        SubscribeLocalEvent<CoughRotChangeModifyComponent, RotUpdateEvent>(OnRotUpdate);
        SubscribeLocalEvent<CoughRotChangeModifyComponent, StartedRottingEvent>(OnStartedRotting);
    }

    private void OnRotGetChance(Entity<CoughRotChangeModifyComponent> ent, ref CoughGetChangceEvent args)
    {
        if (!ent.Comp.Enabled)
        {
            args.Cancel();
            return;
        }

        args.Chance *= ent.Comp.CurrentMutliplier;
    }

    private void OnRotUpdate(Entity<CoughRotChangeModifyComponent> ent, ref RotUpdateEvent args)
    {
        ent.Comp.CurrentMutliplier = ent.Comp.HealthyMultiplier + args.RotProgress * (ent.Comp.DamagedMultiplier - ent.Comp.HealthyMultiplier);

        if (!ent.Comp.DisabledOnRot)
            return;

        ent.Comp.Enabled = args.RotProgress < 1f;
    }

    private void OnStartedRotting(Entity<CoughRotChangeModifyComponent> ent, ref StartedRottingEvent args)
    {
        if (!ent.Comp.DisabledOnRot)
            return;

        ent.Comp.Enabled = false;
    }

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
