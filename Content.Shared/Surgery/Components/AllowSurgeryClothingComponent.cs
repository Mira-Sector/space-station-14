using Content.Shared.Body.Part;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AllowSurgeryClothingComponent : Component, IClothingSlots
{
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.WITHOUT_POCKET;

    [DataField]
    public HashSet<BodyPartType> BodyParts = [];
}
