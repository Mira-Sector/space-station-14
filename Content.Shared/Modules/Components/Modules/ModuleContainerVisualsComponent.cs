using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModuleContainerVisualsComponent : BaseToggleableModuleComponent
{
    [DataField, AutoNetworkedField]
    public Dictionary<bool, Dictionary<string, List<PrototypeLayerData>>> ClothingLayers = [];

    [DataField, AutoNetworkedField]
    public Dictionary<bool, List<PrototypeLayerData>> ItemLayers = [];

    [DataField, AutoNetworkedField]
    public Dictionary<bool, Dictionary<HandLocation, List<PrototypeLayerData>>> InHandLayers = [];

    [ViewVariables]
    public HashSet<int> RevealedIconVisuals = [];
}
