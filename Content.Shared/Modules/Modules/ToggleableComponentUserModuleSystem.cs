using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableComponentUserModuleSystem : EntitySystem
{
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableComponentUserModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<ToggleableComponentUserModuleComponent, ModuleDisabledEvent>(OnDisabled);
    }

    private void OnEnabled(Entity<ToggleableComponentUserModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_moduleContained.TryGetUser(ent.Owner, out var user))
            return;

        EntityManager.AddComponents(user.Value, ent.Comp.Components);
    }

    private void OnDisabled(Entity<ToggleableComponentUserModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_moduleContained.TryGetUser(ent.Owner, out var user))
            return;

        EntityManager.RemoveComponents(user.Value, ent.Comp.Components);
    }
}
