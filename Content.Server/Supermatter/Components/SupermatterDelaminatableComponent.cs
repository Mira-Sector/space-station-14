using Content.Server.Supermatter.Delaminations;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterDelaminatableComponent : Component
{
    [DataField]
    public List<SupermatterDelamination> Delaminations = new();
}

[DataDefinition]
public sealed partial class SupermatterDelamination
{
    [DataField]
    public SupermatterDelaminationType[] Delaminations;

    [DataField]
    public HashSet<DelaminationRequirement> Requirements = new();
}
