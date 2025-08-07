using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;

namespace Content.Shared.Modules.Modules;

public sealed partial class ModSuitDeployableContainerVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedModuleSystem _module = default!;
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitDeployableContainerVisualsComponent, ModuleContainerVisualsGetVisualEntityEvent>(OnGetVisualEntity);
    }

    private void OnGetVisualEntity(Entity<ModSuitDeployableContainerVisualsComponent> ent, ref ModuleContainerVisualsGetVisualEntityEvent args)
    {
        if (!_module.TryGetContainer(ent.Owner, out var container))
            return;

        if (_modSuit.TryGetDeployedPart(container.Value, ent.Comp.PartType, out var foundPart))
            args.Entity = foundPart;
    }
}
