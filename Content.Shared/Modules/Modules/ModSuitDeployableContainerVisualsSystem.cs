using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;

namespace Content.Shared.Modules.Modules;

public sealed partial class ModSuitDeployableContainerVisualsSystem : EntitySystem
{
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitDeployableContainerVisualsComponent, ModuleContainerVisualsGetVisualEntityEvent>(OnGetVisualEntity);
    }

    private void OnGetVisualEntity(Entity<ModSuitDeployableContainerVisualsComponent> ent, ref ModuleContainerVisualsGetVisualEntityEvent args)
    {
        if (!_moduleContained.TryGetContainer(ent.Owner, out var container))
            return;

        if (IsPart(ent, container.Value))
        {
            args.Entity = container;
            return;
        }

        foreach (var part in _modSuit.GetAllParts(container.Value))
        {
            if (!IsPart(ent, part))
                continue;

            args.Entity = part;
            return;
        }
    }

    internal bool IsPart(Entity<ModSuitDeployableContainerVisualsComponent> ent, EntityUid part)
    {
        return CompOrNull<ModSuitPartTypeComponent>(part)?.Type == ent.Comp.PartType;
    }
}
