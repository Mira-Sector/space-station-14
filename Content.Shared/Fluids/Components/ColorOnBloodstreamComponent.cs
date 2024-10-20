using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ColorOnBloodstreamComponent : Component
{
}

[Serializable, NetSerializable]
public enum BloodColor
{
    Color
}
