using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using System.Linq;

namespace Content.Shared.Modules.Modules;

public sealed partial class RequireSealedModuleSystem : BaseToggleableModuleSystem<RequireSealedModuleComponent>
{
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequireSealedModuleComponent, ModuleRelayedEvent<ModSuitContainerPartSealedEvent>>(OnSealed);
        SubscribeLocalEvent<RequireSealedModuleComponent, ModuleRelayedEvent<ModSuitContainerPartUnsealedEvent>>(OnUnsealed);
    }

    protected override void OnToggleAttempt(Entity<RequireSealedModuleComponent> ent, ref ModuleToggleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanEnable(ent, args.Container))
            args.Cancel();
    }

    private void OnSealed(Entity<RequireSealedModuleComponent> ent, ref ModuleRelayedEvent<ModSuitContainerPartSealedEvent> args)
    {
        if (!ent.Comp.EnableOnSealed)
            return;

        if (CanEnable(ent, args.ModuleOwner))
            return;

        var ev = new ModuleDisabledEvent(args.ModuleOwner, null);
        RaiseLocalEvent(ent.Owner, ev);
    }

    private void OnUnsealed(Entity<RequireSealedModuleComponent> ent, ref ModuleRelayedEvent<ModSuitContainerPartUnsealedEvent> args)
    {
        if (!CanEnable(ent, args.ModuleOwner))
            return;

        var ev = new ModuleEnabledEvent(args.ModuleOwner, null);
        RaiseLocalEvent(ent.Owner, ev);
    }

    private bool CanEnable(Entity<RequireSealedModuleComponent> ent, EntityUid container)
    {
        var parts = _modSuit.GetDeployedParts(container);
        var remainingTypes = ent.Comp.Parts;

        foreach (var part in parts)
        {
            var type = Comp<ModSuitPartTypeComponent>(part).Type;
            if (!remainingTypes.Remove(type))
                continue;

            if (!ent.Comp.RequireAll)
                return true;
        }

        return !remainingTypes.Any();
    }
}
