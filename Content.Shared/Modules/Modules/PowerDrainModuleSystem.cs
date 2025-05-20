using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;

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
