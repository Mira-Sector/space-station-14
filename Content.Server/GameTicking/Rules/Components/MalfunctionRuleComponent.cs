using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(MalfunctionRuleSystem))]
public sealed partial class MalfunctionRuleComponent : Component
{
    [DataField]
    public List<ProtoId<SiliconLawPrototype>> Laws = new()
    {
        "Malfunction0"
    };
}
