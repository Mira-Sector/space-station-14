using Content.Shared.FixedPoint;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterModifyIntegerityOnEnergyComponent : Component
{
    [DataField]
    public float Min  = float.MinValue;

    [DataField]
    public float Max  = float.MaxValue;

    [DataField]
    public FixedPoint2 IntegerityPerEnergy;
}
