using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;
using JetBrains.Annotations;

namespace Content.Shared.Modules.Modules;

public sealed partial class FlashlightModuleSystem : BaseToggleableUiModuleSystem<FlashlightModuleComponent>
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedModuleContainerVisualsSystem _moduleContainerVisuals = default!;

    internal Dictionary<NetEntity, TimeSpan> ColorUpdates = [];
    internal static readonly TimeSpan ColorDelay = TimeSpan.FromSeconds(0.125f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ModSuitFlashlightColorChangedMessage>(OnFlashlightColorChanged);
    }

    protected override void OnEnabled(Entity<FlashlightModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);

        if (ent.Comp.Container is not { } container)
            return;

        var pointlight = _pointLight.EnsureLight(container);
        _pointLight.SetColor(container, ent.Comp.Color, pointlight);
        _pointLight.SetEnabled(container, true, pointlight);
    }

    protected override void OnDisabled(Entity<FlashlightModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);

        if (ent.Comp.Container is not { } container)
            return;

        _pointLight.SetEnabled(container, false);
    }

    protected override ModSuitBaseModuleBuiEntry GetModSuitModuleBuiEntry(Entity<FlashlightModuleComponent> ent)
    {
        return new ModSuitFlashlightModuleBuiEntry(ent.Comp.Toggled, ent.Comp.Color, CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity);
    }

    private void OnFlashlightColorChanged(ModSuitFlashlightColorChangedMessage args)
    {
        if (ColorUpdates.TryGetValue(args.Module, out var nextUpdate))
        {
            if (nextUpdate > Timing.CurTime)
                return;
        }

        nextUpdate = Timing.CurTime + ColorDelay;
        NextUpdate[args.Module] = nextUpdate;

        SetColor(GetEntity(args.Module), args.Color);
    }

    [PublicAPI]
    public void SetColor(Entity<FlashlightModuleComponent?> ent, Color color)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Color = color;
        Dirty(ent);

        if (ent.Comp.Container is not { } container)
            return;

        _pointLight.SetColor(container, color);

        if (TryComp<ModuleContainerVisualsComponent>(ent.Owner, out var containerVisuals))
        {
            foreach (var (_, slotsLayers) in containerVisuals.ClothingLayers)
            {
                foreach (var (_, slotLayers) in slotsLayers)
                {
                    foreach (var layer in slotLayers)
                        layer.Color = color;
                }
            }

            foreach (var (_, itemLayers) in containerVisuals.ItemLayers)
            {
                foreach (var layer in itemLayers)
                    layer.Color = color;
            }

            foreach (var (_, handsLayers) in containerVisuals.InHandLayers)
            {
                foreach (var (_, handLayers) in handsLayers)
                {
                    foreach (var layer in handLayers)
                        layer.Color = color;
                }
            }

            Dirty(ent.Owner, containerVisuals);
            _moduleContainerVisuals.UpdateVisuals((ent.Owner, containerVisuals));
        }
    }
}
