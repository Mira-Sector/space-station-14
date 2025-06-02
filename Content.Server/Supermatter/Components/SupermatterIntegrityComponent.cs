using Content.Shared.FixedPoint;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterIntegerityComponent : Component
{
    [DataField]
    public FixedPoint2 MaxIntegrity = 500;

    [ViewVariables, Access(typeof(SupermatterSystem), Other = AccessPermissions.Read)]
    public FixedPoint2 Integerity;

    [ViewVariables]
    public bool IsDelaminating;
}
