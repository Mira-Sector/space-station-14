using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableUiModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;
    [Dependency] private readonly SharedModuleSystem _module = default!;
    [Dependency] private readonly ToggleableModuleSystem _toggleableModule = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableUiModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<ToggleableUiModuleComponent, ModuleDisabledEvent>(OnDisabled);

        SubscribeLocalEvent<ToggleableUiModuleComponent, ModSuitGetModuleUiEvent>(OnGetModSuitUi);

        SubscribeAllEvent<ModSuitToggleButtonMessage>(OnToggleButton);
    }

    private void OnToggleButton(ModSuitToggleButtonMessage args)
    {
        var module = GetEntity(args.Module);

        if (!_module.TryGetContainer(module, out var container))
            return;

        if (!CanUpdate(module))
            return;

        _toggleableModule.Toggle(module, args.Toggle, GetEntity(args.User));
        _modSuit.UpdateUI(container.Value);
    }

    private void OnEnabled(Entity<ToggleableUiModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        if (_module.TryGetContainer(ent.Owner, out var container) && CanUpdate(container.Value))
            _modSuit.UpdateUI(container.Value);
    }

    private void OnDisabled(Entity<ToggleableUiModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        if (_module.TryGetContainer(ent.Owner, out var container) && CanUpdate(container.Value))
            _modSuit.UpdateUI(container.Value);
    }

    private void OnGetModSuitUi(Entity<ToggleableUiModuleComponent> ent, ref ModSuitGetModuleUiEvent args)
    {
        args.BuiEntries.Add(new ModSuitBaseToggleableModuleBuiEntry(
                _toggleableModule.IsToggled(ent.Owner),
                CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity
            )
        );
    }

    private bool CanUpdate(Entity<ToggleableUiModuleComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (ent.Comp.NextButtonPress > _timing.RealTime)
            return false;

        ent.Comp.NextButtonPress = _timing.RealTime + ent.Comp.ButtonDelay;
        Dirty(ent);
        return true;
    }
}
