using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using JetBrains.Annotations;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    [PublicAPI]
    public override void UpdateUI(Entity<ModSuitUserInterfaceComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!Ui.IsUiOpen(ent.Owner, ModSuitUiKey.Key))
            return;

        var ev = new ModSuitGetUiStatesEvent();
        RaiseLocalEvent(ent.Owner, ev);

        foreach (var state in ev.States)
            Ui.SetUiState(ent.Owner, ModSuitUiKey.Key, state);
    }
}
