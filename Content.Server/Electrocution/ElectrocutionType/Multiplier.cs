using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Electrocution.Types;

[UsedImplicitly]
[Serializable]
public sealed partial class Multiplier : ElectrocutionType
{
    [DataField]
    public float DamageMultiplier = 0.00004f;

    [DataField]
    public float TimeMultiplier = 0.0000001f;

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
        damage = (int) ((node.NetworkNode.LastCombinedSupply * DamageMultiplier) + electrified.ShockDamage);
        time = (int) ((node.NetworkNode.LastCombinedSupply * TimeMultiplier) + electrified.ShockTime);
    }
}
