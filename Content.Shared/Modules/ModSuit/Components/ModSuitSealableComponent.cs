using Robust.Shared.GameStates;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitSealableComponent : Component
{
    [DataField, AutoNetworkedField, Access(typeof(SharedModSuitSystem))]
    public bool Sealed;

    [DataField, AutoNetworkedField]
    public Dictionary<bool, List<PrototypeLayerData>> IconLayers = [];

    [DataField, AutoNetworkedField]
    public Dictionary<bool, Dictionary<string, List<PrototypeLayerData>>> ClothingLayers = [];

    public HashSet<int> RevealedLayers = [];
}
