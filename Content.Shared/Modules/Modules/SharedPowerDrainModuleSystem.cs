using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.PowerCell;

namespace Content.Shared.Modules.Modules;

public abstract partial class SharedPowerDrainModuleSystem : BaseToggleableModuleSystem<PowerDrainModuleComponent>
{
    [Dependency] private readonly SharedModuleSystem _module = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerDrainModuleComponent, GetModulePowerDrawEvent>(OnGetPower);
        SubscribeLocalEvent<PowerDrainModuleComponent, ModuleRelayedEvent<PowerCellSlotEmptyEvent>>(OnEmpty);
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
        float? entry;

        if (ent.Comp.Toggled)
            entry = ent.Comp.EnabledDraw;
        else
            entry = ent.Comp.DisabledDraw;

        if (entry == null)
            return;

        args.Additional += entry.Value;
    }

    private void OnEmpty(Entity<PowerDrainModuleComponent> ent, ref ModuleRelayedEvent<PowerCellSlotEmptyEvent> args)
    {
        RaiseToggleEvents(ent, false, null);
    }
}
