using Content.Client.Modules.ModSuit.Events;
using Content.Shared.Clothing;
using Content.Shared.Item;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;
using System.Linq;

namespace Content.Client.Modules.ModSuit;

public partial class ModSuitSystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private const string LayerPrefix = "modsuit-sealable-layers";

    private void InitializeSealable()
    {
        SubscribeLocalEvent<ModSuitSealableComponent, AppearanceChangeEvent>(OnSealableAppearanceChange);
        SubscribeLocalEvent<ModSuitSealableComponent, GetEquipmentVisualsEvent>(OnSealableClothingVisuals);
    }

    private void OnSealableAppearanceChange(Entity<ModSuitSealableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!Appearance.TryGetData<bool>(ent.Owner, ModSuitSealedVisuals.Sealed, out var isSealed))
            return;

        _item.VisualsChanged(ent.Owner);

        if (args.Sprite is not { } sprite)
            return;

        foreach (var layer in ent.Comp.RevealedLayers)
            sprite.RemoveLayer(layer);

        ent.Comp.RevealedLayers.Clear();

        if (!ent.Comp.IconLayers.TryGetValue(isSealed, out var layers))
            return;

        var ev = new ModSuitSealedGetIconLayersEvent();
        RaiseLocalEvent(ent.Owner, ev);

        AddIconLayers(ent, layers, sprite);
        AddIconLayers(ent, ev.Layers, sprite);
    }

    private void AddIconLayers(Entity<ModSuitSealableComponent> ent, List<PrototypeLayerData> layers, SpriteComponent sprite)
    {
        foreach (var layer in layers)
        {
            layer.MapKeys ??= [];
            layer.MapKeys.Add(LayerPrefix);

            ent.Comp.RevealedLayers.Add(sprite.AddLayer(layer));
        }
    }

    private void OnSealableClothingVisuals(Entity<ModSuitSealableComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!Appearance.TryGetData<bool>(ent.Owner, ModSuitSealedVisuals.Sealed, out var isSealed))
            return;

        if (!ent.Comp.ClothingLayers.TryGetValue(isSealed, out var clothingLayers))
            return;

        if (!clothingLayers.TryGetValue(args.Slot, out var layers))
            return;

        var ev = new ModSuitSealedGetClothingLayersEvent(args.Slot);
        RaiseLocalEvent(ent.Owner, ev);

        AddClothingLayers(layers, ref args);
        AddClothingLayers(ev.Layers, ref args);
    }

    private void AddClothingLayers(List<PrototypeLayerData> layers, ref GetEquipmentVisualsEvent args)
    {
        List<(string, PrototypeLayerData)> toAdd = [];

        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];

            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = $"{LayerPrefix}-{args.Slot}-{i + args.Layers.Count}";
            }
            else
            {
                key = $"{LayerPrefix}-{args.Slot}-{key}";
                layer.MapKeys = [key];
            }

            toAdd.Add((key, layer));
        }

        args.Layers.AddRange(toAdd);
    }
}
