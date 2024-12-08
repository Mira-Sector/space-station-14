using Content.Shared.Electrocution;
using Content.Server.Power.NodeGroups;
using Robust.Shared.Prototypes;

namespace Content.Server.Electrocution;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class ElectrocutionType
{
    public abstract void Electrocution(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        EntityUid shocked,
        EntityUid shocker,
        ElectrifiedComponent electrified,
        IBasePowerNet node,
        out int damage,
        out float time);
}
