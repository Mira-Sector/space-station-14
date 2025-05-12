using Robust.Shared.Containers;

namespace Content.Server.Modules.ModSuit.Components;

[RegisterComponent]
public sealed partial class ModSuitDeployableInventoryComponent : Component
{
    [DataField]
    public string ContainerId = "mod_suit_inventory";

    [ViewVariables]
    public Container? StoredItem;
}
