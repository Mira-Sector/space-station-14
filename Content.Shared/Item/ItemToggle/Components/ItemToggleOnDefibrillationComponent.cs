using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleOnDefibrillationComponent : Component
{
    [DataField]
    public bool Enable;
}
