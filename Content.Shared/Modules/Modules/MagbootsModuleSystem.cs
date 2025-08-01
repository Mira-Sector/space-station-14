using Content.Shared.Clothing;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;

namespace Content.Shared.Modules.Modules;

public sealed partial class MagbootsModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedMagbootsSystem _magboots = default!;
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsModuleComponent, ModuleEnabledEvent>(OnEnabled, after: [typeof(ToggleableComponentModSuitPartModuleSystem)]);
        SubscribeLocalEvent<MagbootsModuleComponent, ModuleDisabledEvent>(OnDisabled, after: [typeof(ToggleableComponentModSuitPartModuleSystem)]);
    }

    private void OnEnabled(Entity<MagbootsModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        UpdateMagboots(ent, args.Container, true);
    }

    private void OnDisabled(Entity<MagbootsModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        UpdateMagboots(ent, args.Container, false);
    }

    private void UpdateMagboots(Entity<MagbootsModuleComponent> module, EntityUid container, bool state)
    {
        var magboots = GetMagboots(module, container);

        if (_moduleContained.TryGetUser(module.Owner, out var user))
            _magboots.UpdateMagbootEffects(user.Value, magboots, state);
    }

    private Entity<MagbootsComponent> GetMagboots(Entity<MagbootsModuleComponent> module, EntityUid container)
    {
        if (module.Comp.ModSuitPart is { } partType)
        {
            if (_modSuit.TryGetDeployedPart(container, partType, out var part))
                return (part.Value, EnsureComp<MagbootsComponent>(part.Value));
        }

        return (container, EnsureComp<MagbootsComponent>(container));
    }
}
