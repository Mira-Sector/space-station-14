using Content.Shared.Atmos;

namespace Content.Server.Supermatter.GasReactions;

[ImplicitDataDefinitionForInheritors]
public abstract partial class SupermatterGasReaction
{
    public abstract bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan);
}
