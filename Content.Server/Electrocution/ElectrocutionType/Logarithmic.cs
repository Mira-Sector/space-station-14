using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Electrocution.Types;

[UsedImplicitly]
[Serializable]
public sealed partial class Logarithmic : ElectrocutionType
{
    [DataField]
    public ElectrocutionLogarathm Damage = new();

    [DataField]
    public ElectrocutionLogarathm Time = new();

    public override void Electrocution(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        EntityUid shocked,
        EntityUid shocker,
        ElectrifiedComponent electrified,
        IBasePowerNet node,
        out int damage,
        out float time)
    {
        damage = (int) Math.Round(Math.Log((node.NetworkNode.LastCombinedSupply * Damage.Growth) + Damage.Shift, Damage.Coefficient));
        time = (float) Math.Round(Math.Log((node.NetworkNode.LastCombinedSupply * Time.Growth) + Time.Shift, Time.Coefficient));
    }
}

[Serializable, DataDefinition]
public partial class ElectrocutionLogarathm
{
    public float Shift { get; set; } = 1.6f;
    public float Coefficient { get; set; } = 1.3f;
    public float Growth { get; set; } = 0.0005f;
}
