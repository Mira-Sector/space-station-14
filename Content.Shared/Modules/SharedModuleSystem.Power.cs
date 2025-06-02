using Content.Shared.Clothing;
using Content.Shared.Modules.Components;
using Content.Shared.Modules.Events;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Modules;

public partial class SharedModuleSystem
{
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<ModuleContainerPowerComponent, ClothingGotEquippedEvent>(OnPowerEquipped);
        SubscribeLocalEvent<ModuleContainerPowerComponent, ClothingGotUnequippedEvent>(OnPowerUnequipped);

        SubscribeLocalEvent<ModuleContainerPowerComponent, ModuleContainerModuleAddedEvent>((u, c, a) => UpdatePowerDraw((u, c)));
        SubscribeLocalEvent<ModuleContainerPowerComponent, ModuleContainerModuleRemovedEvent>((u, c, a) => UpdatePowerDraw((u, c)));

        SubscribeLocalEvent<ModuleContainerPowerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnPowerEquipped(Entity<ModuleContainerPowerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _powerCell.SetDrawEnabled(ent.Owner, true);
        UpdatePowerDraw((ent.Owner, ent.Comp));
    }

    private void OnPowerUnequipped(Entity<ModuleContainerPowerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _powerCell.SetDrawEnabled(ent.Owner, false);
    }

    private void OnPowerCellChanged(Entity<ModuleContainerPowerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (ent.Comp.NextUiUpdate > _timing.CurTime)
            return;

        ent.Comp.NextUiUpdate += ent.Comp.UiUpdateInterval;
        Dirty(ent);
        UpdateUis(ent.Owner);
    }

    [PublicAPI]
    public void UpdatePowerDraw(Entity<ModuleContainerPowerComponent?, ModuleContainerComponent?, PowerCellDrawComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp3))
            return;

        ent.Comp3.DrawRate = GetModulesPowerDraw((ent.Owner, ent.Comp1, ent.Comp2));
    }

    [PublicAPI]
    public float GetModulesPowerDraw(Entity<ModuleContainerPowerComponent?, ModuleContainerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1))
            return 0f;

        var ev = new GetModulePowerDrawEvent();
        RaiseEventToModules((ent.Owner, ent.Comp2), ev);

        var draw = ent.Comp1.BaseRate + ev.Additional;
        return draw;
    }

    [PublicAPI]
    public float GetBaseRate(Entity<ModuleContainerPowerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return 0f;

        return ent.Comp.BaseRate;
    }
}
