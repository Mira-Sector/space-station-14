using Content.Server.PowerCell;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.Modules;

namespace Content.Server.Modules.Modules;

public sealed partial class PowerDrainModuleSystem : SharedPowerDrainModuleSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    protected override void OnEnabled(Entity<PowerDrainModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);

        if (ent.Comp.OnUseDraw == null)
            return;

        _powerCell.TryUseCharge(args.Container, ent.Comp.OnUseDraw.Value);
    }
}
