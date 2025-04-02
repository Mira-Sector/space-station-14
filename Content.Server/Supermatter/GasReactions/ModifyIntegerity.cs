using Content.Shared.Atmos;
using Content.Shared.FixedPoint;

namespace Content.Server.Supermatter.GasReactions;

[DataDefinition]
public sealed partial class ModifyIntegerity : SupermatterGasReaction
{
    /// <summary>
    /// What to use in the case this is space
    /// </summary>
    [DataField]
    public FixedPoint2 Integerity;

    public override bool React(EntityUid supermatter, Gas? gas, GasMixture air, TimeSpan lastReaction, IEntityManager entMan)
    {
        var integerity = gas == null ? Integerity : (air.GetMoles(gas.Value) / air.TotalMoles);
        integerity *= lastReaction.TotalSeconds;
        var supermatterSys = entMan.System<SupermatterSystem>();
        supermatterSys.ModifyIntegerity(supermatter, integerity);
        return true;
    }
}
