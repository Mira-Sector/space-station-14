using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Reflection;

namespace Content.Shared.Dyable;

public abstract partial class SharedDyableSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
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
    }

    private void UpdateClothing(Entity<DyableComponent> ent)
    {
        var key = _reflection.GetEnumReference(DyableVisualsLayers.Layer);

        if (TryComp<ClothingComponent>(ent.Owner, out var clothing))
        {
            foreach (var slot in clothing.ClothingVisuals.Keys)
                _clothing.SetLayerColor(clothing, slot, key, ent.Comp.Color);
        }
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
    }
}
