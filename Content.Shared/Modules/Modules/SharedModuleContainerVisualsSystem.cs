using Content.Shared.Item;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using JetBrains.Annotations;

namespace Content.Shared.Modules.Modules;

public abstract partial class SharedModuleContainerVisualsSystem : BaseToggleableModuleSystem<ModuleContainerVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnEnabled(Entity<ModuleContainerVisualsComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);

        if (GetVisualEntity(ent) is not { } visual)
            return;

        _item.VisualsChanged(visual);
    }

    protected override void OnDisabled(Entity<ModuleContainerVisualsComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);

        if (GetVisualEntity(ent) is not { } visual)
            return;

        _item.VisualsChanged(visual);
    }

    [PublicAPI]
    public void UpdateVisuals(Entity<ModuleContainerVisualsComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (GetVisualEntity((ent.Owner, ent.Comp)) is not { } visual)
            return;

        _item.VisualsChanged(visual);
    }

    internal EntityUid? GetVisualEntity(Entity<ModuleContainerVisualsComponent> ent)
    {
        var ev = new ModuleContainerVisualsGetVisualEntityEvent();
        RaiseLocalEvent(ent.Owner, ev);

        return ev.Entity ?? ent.Comp.Container;
    }
}
