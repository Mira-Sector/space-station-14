using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Shared.Reflection;

namespace Content.Shared.Dyable;

public abstract partial class SharedDyableSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DyableComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<DyableComponent> ent, ref ComponentInit args)
    {
        Appearance.SetData(ent.Owner, DyableVisuals.Color, ent.Comp.Color);
        UpdateClothing(ent);
        UpdateItem(ent);
        UpdateForensics(ent);
    }

    private void UpdateClothing(Entity<DyableComponent> ent)
    {
        if (!TryComp<ClothingComponent>(ent.Owner, out var clothing))
            return;

        var key = _reflection.GetEnumReference(DyableVisualsLayers.Layer);

        foreach (var slot in clothing.ClothingVisuals.Keys)
            _clothing.SetLayerColor(clothing, slot, key, ent.Comp.Color);
    }

    private void UpdateItem(Entity<DyableComponent> ent)
    {
        if (!TryComp<ItemComponent>(ent.Owner, out var item))
            return;

        var key = _reflection.GetEnumReference(DyableVisualsLayers.Layer);

        foreach (var hand in item.InhandVisuals.Keys)
            _item.SetLayerColor(ent.Owner, hand, key, ent.Comp.Color, item);
    }

    protected virtual void UpdateForensics(Entity<DyableComponent> ent)
    {
    }

    [PublicAPI]
    public void SetColor(Entity<DyableComponent?> ent, Color color)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Color = color;
        DirtyField(ent.Owner, ent.Comp, nameof(DyableComponent.Color));
        Appearance.SetData(ent.Owner, DyableVisuals.Color, ent.Comp.Color);
        UpdateClothing((ent.Owner, ent.Comp));
        UpdateItem((ent.Owner, ent.Comp));
        UpdateForensics((ent.Owner, ent.Comp));
    }
}
