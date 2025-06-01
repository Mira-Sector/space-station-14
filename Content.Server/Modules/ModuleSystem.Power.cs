using Content.Server.PowerCell;
using Content.Shared.Modules.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;

namespace Content.Server.Modules;

public partial class ModuleSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<ModuleContainerPowerComponent, ModSuitGetUiEntriesEvent>(OnModSuitGetUi);
    }

    private void OnModSuitGetUi(Entity<ModuleContainerPowerComponent> ent, ref ModSuitGetUiEntriesEvent args)
    {
        BaseModSuitPowerBuiEntry entry;

        if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
            entry = new ModSuitPowerBuiEntry(battery.CurrentCharge, battery.MaxCharge);
        else
            entry = new ModSuitPowerNoCellBuiEntry();

        args.Entries.Add(entry);
    }
}
