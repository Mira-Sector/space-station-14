using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Allows moving a <see cref="StationAiCoreComponent"/> contained entity to and from this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiHolderComponent : Component
{
    public const string Container = StationAiCoreComponent.Container;

    [DataField]
    public ItemSlot Slot = new();

    [DataField]
    public bool UpdateSprite = true;

    [DataField]
    public Dictionary<StationAiState, Dictionary<StationAiVisualLayers, SpriteSpecifier>> Visuals = new();
}
