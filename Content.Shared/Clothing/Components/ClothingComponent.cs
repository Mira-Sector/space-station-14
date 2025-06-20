using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This handles entities which can be equipped.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
[Access(typeof(ClothingSystem), typeof(InventorySystem))]
public sealed partial class ClothingComponent : Component
{
    [DataField("clothingVisuals")]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    /// <summary>
    /// The name of the layer in the user that this piece of clothing will map to
    /// </summary>
    [DataField]
    public string? MappedLayer;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("quickEquip")]
    public bool QuickEquip = true;

    /// <summary>
    /// The slots in which the clothing is considered "worn" or "equipped". E.g., putting shoes in your pockets does not
    /// equip them as far as clothing related events are concerned.
    /// </summary>
    /// <remarks>
    /// Note that this may be a combination of different slot flags, not a singular bit.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    [Access(typeof(ClothingSystem), typeof(InventorySystem), Other = AccessPermissions.ReadExecute)]
    public SlotFlags Slots = SlotFlags.NONE;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("equipSound")]
    public SoundSpecifier? EquipSound;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("unequipSound")]
    public SoundSpecifier? UnequipSound;

    [Access(typeof(ClothingSystem))]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("equippedPrefix")]
    public string? EquippedPrefix;

    /// <summary>
    /// Allows the equipped state to be directly overwritten.
    /// useful when prototyping INNERCLOTHING items into OUTERCLOTHING items without duplicating/modifying RSIs etc.
    /// </summary>
    [Access(typeof(ClothingSystem))]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("equippedState")]
    public string? EquippedState;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprite")]
    public string? RsiPath;

    [DataField]
    public bool AutoGenerateVisuals = true;

    /// <summary>
    /// Name of the inventory slot the clothing is currently in.
    /// Note that this being non-null does not mean the clothing is considered "worn" or "equipped" unless the slot
    /// satisfies the <see cref="Slots"/> flags.
    /// </summary>
    [DataField]
    public string? InSlot;
    // TODO CLOTHING
    // Maybe keep this null unless its in a valid slot?
    // To lazy to figure out ATM if that would break anything.
    // And when doing this, combine InSlot and InSlotFlag, as it'd be a breaking change for downstreams anyway

    /// <summary>
    /// Slot flags of the slot the clothing is currently in. See also <see cref="InSlot"/>.
    /// </summary>
    [DataField]
    public SlotFlags? InSlotFlag;
    // TODO CLOTHING
    // Maybe keep this null unless its in a valid slot?
    // And when doing this, combine InSlot and InSlotFlag, as it'd be a breaking change for downstreams anyway

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EquipDelay = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UnequipDelay = TimeSpan.Zero;

    /// <summary>
    /// Offset for the strip time for an entity with this component.
    /// Only applied when it is being equipped or removed by another player.
    /// </summary>
    [DataField]
    public TimeSpan StripDelay = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed class ClothingComponentState : ComponentState
{
    public string? EquippedPrefix;

    public ClothingComponentState(string? equippedPrefix)
    {
        EquippedPrefix = equippedPrefix;
    }
}

public enum ClothingMask : byte
{
    NoMask = 0,
    UniformFull,
    UniformTop
}

[Serializable, NetSerializable]
public sealed partial class ClothingEquipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public ClothingEquipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ClothingUnequipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public ClothingUnequipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}
