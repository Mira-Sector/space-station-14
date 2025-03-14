using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Layerable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PipeLayerableComponent : Component
{
    [DataField]
    public int MaxLayer = 2;

    [DataField]
    public int MinLayer = -2;

    [ViewVariables, AutoNetworkedField]
    public int Layer;
}
