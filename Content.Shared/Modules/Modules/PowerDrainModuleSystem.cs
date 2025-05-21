using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.PowerCell;

namespace Content.Shared.Modules.Modules;

public sealed partial class PowerDrainModuleSystem : BaseToggleableModuleSystem<PowerDrainModuleComponent>
{
    [Dependency] private readonly ModuleSystem _module = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerDrainModuleComponent, GetModulePowerDrawEvent>(OnGetPower);
    }

    protected override void OnEnabled(Entity<PowerDrainModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);
        _module.UpdatePowerDraw(args.Container);

        if (ent.Comp.OnUseDraw == null)
            return;

        if (!TryComp<PowerCellDrawComponent>(args.Container, out var powerDraw))
            return;

        var rate = _module.GetBaseRate(args.Container);
        rate += ent.Comp.OnUseDraw.Additional;
        rate *= ent.Comp.OnUseDraw.Multiplier;

        powerDraw.DrawRate = rate;
    }

    protected override void OnDisabled(Entity<PowerDrainModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);
        _module.UpdatePowerDraw(args.Container);
    }

    private void OnGetPower(Entity<PowerDrainModuleComponent> ent, ref GetModulePowerDrawEvent args)
    {
        PowerDrainEntry? entry;

        if (ent.Comp.Toggled)
            entry = ent.Comp.EnabledDraw;
        else
            entry = ent.Comp.DisabledDraw;

        if (entry == null)
            return;

        args.Additional += entry.Additional;
        args.Multiplier += entry.Multiplier;
    }
}
