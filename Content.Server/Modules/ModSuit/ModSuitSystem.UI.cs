using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    public override void UpdateUI(Entity<ModSuitUserInterfaceComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!Ui.IsUiOpen(ent.Owner, ModSuitUiKey.Key))
            return;

        var ev = new ModSuitGetUiStatesEvent();
        RaiseLocalEvent(ev);

        foreach (var state in ev.States)
            Ui.SetUiState(ent.Owner, ModSuitUiKey.Key, state);
    }
}
