using Content.Client.Modules.ModSuit.Events;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.Modules;
using Robust.Client.GameObjects;

namespace Content.Client.Modules.Modules;

public sealed partial class FlashlightModuleSystem : SharedFlashlightModuleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Type[] after = [typeof(ModuleContainerVisualsSystem)];

        // holy fuck these are longer than me :3
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<AppearanceChangeEvent>>((u, c, a) => OnAppearanceChange((u, c), ref a.Args), after: after);
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<GetEquipmentVisualsEvent>>((u, c, a) => OnGetVisuals((u, c), ref a.Args), after: after);
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<GetInhandVisualsEvent>>((u, c, a) => OnItemVisuals((u, c), ref a.Args), after: after);

        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<ModSuitDeployedPartRelayedEvent<AppearanceChangeEvent>>>((u, c, a) => OnDeployedAppearanceChange((u, c), ref a.Args), after: after);
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<ModSuitDeployedPartRelayedEvent<GetEquipmentVisualsEvent>>>((u, c, a) => OnDeployedGetVisuals((u, c), ref a.Args), after: after);
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<ModSuitDeployedPartRelayedEvent<GetInhandVisualsEvent>>>((u, c, a) => OnItemVisuals((u, c), ref a.Args.Args), after: after);

        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<ModSuitDeployedPartRelayedEvent<ModSuitSealedGetClothingLayersEvent>>>((u, c, a) => OnSealedGetVisuals((u, c), ref a.Args.Args), after: after);
        SubscribeLocalEvent<FlashlightModuleComponent, ModuleRelayedEvent<ModSuitDeployedPartRelayedEvent<ModSuitSealedGetIconLayersEvent>>>((u, c, a) => OnSealedGetIconVisuals((u, c), ref a.Args.Args), after: after);
    }

    private void OnDeployedAppearanceChange(Entity<FlashlightModuleComponent> ent, ref ModSuitDeployedPartRelayedEvent<AppearanceChangeEvent> args)
    {
        // handled in a separate event
        if (HasComp<ModSuitSealableComponent>(args.Part))
            return;

        OnAppearanceChange(ent, ref args.Args);
    }

    private void OnAppearanceChange(Entity<FlashlightModuleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!TryComp<ModuleContainerVisualsComponent>(ent.Owner, out var containerVisuals))
            return;

        foreach (var layerId in containerVisuals.RevealedIconVisuals)
            sprite.LayerSetColor(layerId, ent.Comp.Color);
    }

    private void OnDeployedGetVisuals(Entity<FlashlightModuleComponent> ent, ref ModSuitDeployedPartRelayedEvent<GetEquipmentVisualsEvent> args)
    {
        // handled in a separate event
        if (HasComp<ModSuitSealableComponent>(args.Part))
            return;

        OnGetVisuals(ent, ref args.Args);
    }

    private static void OnGetVisuals(Entity<FlashlightModuleComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        foreach (var (_, layer) in args.Layers)
            layer.Color = ent.Comp.Color;
    }

    private static void OnItemVisuals(Entity<FlashlightModuleComponent> ent, ref GetInhandVisualsEvent args)
    {
        foreach (var (_, layer) in args.Layers)
            layer.Color = ent.Comp.Color;
    }

    private static void OnSealedGetVisuals(Entity<FlashlightModuleComponent> ent, ref ModSuitSealedGetClothingLayersEvent args)
    {
        foreach (var layer in args.Layers)
            layer.Color = ent.Comp.Color;
    }

    private static void OnSealedGetIconVisuals(Entity<FlashlightModuleComponent> ent, ref ModSuitSealedGetIconLayersEvent args)
    {
        foreach (var layer in args.Layers)
            layer.Color = ent.Comp.Color;
    }
}
