using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void UpdateUI(Entity<ModSuitUserInterfaceComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!_ui.IsUiOpen(ent.Owner, ModSuitUiKey.Key))
            return;

        var ev = new ModSuitGetUiStatesEvent();
        RaiseLocalEvent(ev);

        foreach (var state in ev.States)
            _ui.SetUiState(ent.Owner, ModSuitUiKey.Key, state);
    }
}
