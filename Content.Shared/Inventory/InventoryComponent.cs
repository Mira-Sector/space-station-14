using Content.Shared.DisplacementMap;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Inventory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(InventorySystem))]
public sealed partial class InventoryComponent : Component
{
    [DataField("templateId", customTypeSerializer: typeof(PrototypeIdSerializer<InventoryTemplatePrototype>))]
    public string TemplateId { get; private set; } = "human";

    [DataField("speciesId")] public string? SpeciesId { get; set; }

    [ViewVariables, AutoNetworkedField]
    public SlotDefinition[] Slots = Array.Empty<SlotDefinition>();

    [ViewVariables]
    public ContainerSlot[] Containers = Array.Empty<ContainerSlot>();

    [DataField]
    public Dictionary<string, DisplacementData> Displacements = new();

    /// <summary>
    /// Alternate displacement maps, which if available, will be selected for the player of the appropriate gender.
    /// </summary>
    [DataField]
    public Dictionary<string, DisplacementData> FemaleDisplacements = new();

    /// <summary>
    /// Alternate displacement maps, which if available, will be selected for the player of the appropriate gender.
    /// </summary>
    [DataField]
    public Dictionary<string, DisplacementData> MaleDisplacements = new();
}
