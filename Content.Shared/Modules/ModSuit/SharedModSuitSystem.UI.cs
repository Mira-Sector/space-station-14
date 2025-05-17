using Content.Shared.Modules.ModSuit.Components;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    private void InitializeUI()
    {
        SubscribeLocalEvent<ModSuitUserInterfaceComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnUiOpened(Entity<ModSuitUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent.AsNullable());
    }

    [PublicAPI]
    public virtual void UpdateUI(Entity<ModSuitUserInterfaceComponent?> ent)
    {
    }
}
