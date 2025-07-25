using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using BreathToolComponent = Content.Shared.Atmos.Components.BreathToolComponent;
using InternalsComponent = Content.Shared.Body.Components.InternalsComponent;

namespace Content.Server.Body.Systems;

public sealed class LungSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public static string LungSolutionName = "Lung";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LungComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<BreathToolComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<BreathToolComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotUnequipped(Entity<BreathToolComponent> ent, ref GotUnequippedEvent args)
    {
        _atmos.DisconnectInternals(ent);
    }

    private void OnGotEquipped(Entity<BreathToolComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ent.Comp.AllowedSlots) == 0)
        {
            return;
        }

        if (TryComp(args.Equipee, out InternalsComponent? internals))
        {
            ent.Comp.ConnectedInternalsEntity = args.Equipee;
            _internals.ConnectBreathTool((args.Equipee, internals), ent);
        }
    }

    private void OnComponentInit(Entity<LungComponent> entity, ref ComponentInit args)
    {
        if (_solutionContainerSystem.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out var solution))
        {
            solution.MaxVolume = 100.0f;
            solution.CanReact = false; // No dexalin lungs
        }
    }

    public void GasToReagent(EntityUid uid, LungComponent lung)
    {
        if (!_solutionContainerSystem.ResolveSolution(uid, lung.SolutionName, ref lung.Solution, out var solution))
            return;

        GasToReagent(lung.Air, solution);
        _solutionContainerSystem.UpdateChemicals(lung.Solution.Value);
    }

    private void GasToReagent(GasMixture gas, Solution solution)
    {
        foreach (var gasId in Enum.GetValues<Gas>())
        {
            var i = (int) gasId;
            var moles = gas[i];
            if (moles <= 0)
                continue;

            var reagent = _atmos.GasReagents[i];
            if (reagent is null)
                continue;

            var amount = moles * Atmospherics.BreathMolesToReagentMultiplier;
            solution.AddReagent(reagent, amount);
        }
    }

    public Solution GasToReagent(GasMixture gas)
    {
        var solution = new Solution();
        GasToReagent(gas, solution);
        return solution;
    }
}
