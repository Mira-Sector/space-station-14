using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleExamineTextComponent : Component
{
    [DataField]
    public LocId? EnabledText;

    [DataField]
    public LocId? DisabledText;
}
