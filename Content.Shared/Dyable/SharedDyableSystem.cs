using JetBrains.Annotations;

namespace Content.Shared.Dyable;

public abstract partial class SharedDyableSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DyableComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<DyableComponent> ent, ref ComponentInit args)
    {
        Appearance.SetData(ent.Owner, DyableVisuals.Color, ent.Comp.Color);
    }

    [PublicAPI]
    public void SetColor(Entity<DyableComponent?> ent, Color color)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Color = color;
        DirtyField(ent.Owner, ent.Comp, nameof(DyableComponent.Color));
        Appearance.SetData(ent.Owner, DyableVisuals.Color, ent.Comp.Color);
    }
}
