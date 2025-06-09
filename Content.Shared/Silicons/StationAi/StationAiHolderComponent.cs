using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Allows moving a <see cref="StationAiCoreComponent"/> contained entity to and from this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiHolderComponent : Component
{
    public const string Container = StationAiCoreComponent.Container;

    [DataField]
    public ItemSlot Slot = new();

    /// <summary>
    /// Are we empty because the AI died.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AiDied = false;
}
