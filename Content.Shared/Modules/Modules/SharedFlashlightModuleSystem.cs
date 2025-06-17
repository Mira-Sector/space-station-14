using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public abstract partial class SharedFlashlightModuleSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] protected readonly SharedModuleContainerVisualsSystem ModuleContainerVisuals = default!;
    [Dependency] private readonly ToggleableModuleSystem _toggleableModule = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashlightModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleDisabledEvent>(OnDisabled);

        SubscribeLocalEvent<FlashlightModuleComponent, ModSuitGetModuleUiEvent>(OnGetModSuitUi);

        SubscribeAllEvent<ModSuitFlashlightColorChangedMessage>(OnFlashlightColorChanged);
    }

    private void OnEnabled(Entity<FlashlightModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        if (!_moduleContained.TryGetContainer(ent.Owner, out var container))
            return;

        var pointlight = _pointLight.EnsureLight(container.Value);
        _pointLight.SetEnabled(container.Value, true, pointlight);

        SetColor(ent.AsNullable(), ent.Comp.Color);
    }

    private void OnDisabled(Entity<FlashlightModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        if (!_moduleContained.TryGetContainer(ent.Owner, out var container))
            return;

        _pointLight.SetEnabled(container.Value, false);
        SetColor(ent.AsNullable(), ent.Comp.Color);
    }

    private void OnGetModSuitUi(Entity<FlashlightModuleComponent> ent, ref ModSuitGetModuleUiEvent args)
    {
        args.BuiEntries.Add(new ModSuitFlashlightModuleBuiEntry(
                _toggleableModule.IsToggled(ent.Owner),
                ent.Comp.Color,
                CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity
            )
        );
    }

    private void OnFlashlightColorChanged(ModSuitFlashlightColorChangedMessage args)
    {
        var module = GetEntity(args.Module);

        if (!TryComp<FlashlightModuleComponent>(module, out var flashlightModule))
            return;

        if (flashlightModule.NextUpdate > _timing.RealTime)
            return;

        flashlightModule.NextUpdate = _timing.RealTime + flashlightModule.UpdateRate; // no need to dirty as setting color does it

        SetColor(GetEntity(args.Module), args.Color);
    }

    [PublicAPI]
    public void SetColor(Entity<FlashlightModuleComponent?> ent, Color color)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Color = color;
        Dirty(ent);

        if (_moduleContained.TryGetContainer(ent.Owner, out var container))
            _pointLight.SetColor(container.Value, color);

        if (ModuleContainerVisuals.GetVisualEntity(ent.Owner) is { } visual)
            Appearance.SetData(visual, FlashlightModuleVisualState.Color, color);
    }
}
