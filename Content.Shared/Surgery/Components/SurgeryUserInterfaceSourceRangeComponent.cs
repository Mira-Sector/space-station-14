using Robust.Shared.GameStates;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryUserInterfaceSourceRangeComponent : Component
{
    [DataField]
    public float Range = 0.5f;

    public const LookupFlags Flags = LookupFlags.Dynamic | LookupFlags.Sundries;
}
