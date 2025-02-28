using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeartComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Beating = true;
}

[Serializable, NetSerializable]
public enum HeartVisuals : byte
{
    Beating
}

[Serializable, NetSerializable]
public enum HeartVisualLayers : byte
{
    Beating
}
