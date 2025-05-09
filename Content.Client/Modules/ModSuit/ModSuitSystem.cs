using Content.Shared.Clothing;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;
using System.Linq;

namespace Content.Client.Modules.ModSuit;

public sealed partial class ModSuitSystem : SharedModSuitSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private string _layerPrefix = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitSealableComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ModSuitSealableComponent, GetEquipmentVisualsEvent>(OnClothingVisuals);

        _layerPrefix = _reflection.GetEnumReference(ModSuitSealedLayers.Layer);
    }

    private void OnAppearanceChange(Entity<ModSuitSealableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!Appearance.TryGetData<bool>(ent.Owner, ModSuitSealedVisuals.Sealed, out var isSealed))
            return;

        if (args.Sprite is not {} sprite)
            return;

        foreach (var layer in ent.Comp.RevealedLayers)
            sprite.RemoveLayer(layer);

        ent.Comp.RevealedLayers.Clear();

        if (!ent.Comp.IconLayers.TryGetValue(isSealed, out var layers))
            return;

        foreach (var layer in layers)
        {
            layer.MapKeys ??= [];
            layer.MapKeys.Add(_layerPrefix);

            ent.Comp.RevealedLayers.Add(sprite.AddLayer(layer));
        }
    }

    private void OnClothingVisuals(Entity<ModSuitSealableComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!Appearance.TryGetData<bool>(ent.Owner, ModSuitSealedVisuals.Sealed, out var isSealed))
            return;

        if (!ent.Comp.ClothingLayers.TryGetValue(isSealed, out var clothingLayers))
            return;

        if (!clothingLayers.TryGetValue(args.Slot, out var layers))
            return;

        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];

            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = $"{_layerPrefix}-{args.Slot}-{i}";
                i++;
            }
            else
            {
                key = $"{_layerPrefix}-{args.Slot}-{key}";
                layer.MapKeys = [key];
            }

            args.Layers.Add((key, layer));
        }
    }
}
