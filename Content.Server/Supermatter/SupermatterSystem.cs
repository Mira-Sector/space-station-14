using Content.Server.Radio.EntitySystems;
using Content.Server.Supermatter.Components;
using Content.Server.Supermatter.Events;
using Content.Server.Supermatter.GasReactions;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server.Supermatter;

public sealed partial class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterIntegerityComponent, ComponentInit>(OnIntegerityInit);
        SubscribeLocalEvent<SupermatterGasReactionComponent, AtmosExposedUpdateEvent>(OnAtmosExposed);
        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterIntegerityModifiedEvent>(OnRadioIntegerityModified);
        SubscribeLocalEvent<SupermatterDelaminatableComponent, SupermatterDelaminatedEvent>(OnDelaminateableDelaminated);

        SubscribeLocalEvent<SupermatterGasEmitterComponent, ComponentInit>(OnGasEmitterInit);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterGasReactedEvent>(OnGasEmitterGasReact);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterSpaceGasReactedEvent>(OnGasEmitterSpaceReact);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var gasEmitterQuery = EntityQueryEnumerator<SupermatterGasEmitterComponent>();
        while (gasEmitterQuery.MoveNext(out var uid, out var gasEmitterComp))
        {
            if (gasEmitterComp.NextSpawn > _timing.CurTime)
                continue;

            gasEmitterComp.NextSpawn += gasEmitterComp.Delay;

            var air = _atmos.GetContainingMixture(uid, true, true);

            if (air == null)
                continue;

            foreach (var (gas, ratio) in gasEmitterComp.Ratios)
            {
                air.AdjustMoles(gas, ratio * gasEmitterComp.CurrentRate);
            }

            air.Temperature += gasEmitterComp.CurrentTemperature;
        }
    }

    private void OnIntegerityInit(Entity<SupermatterIntegerityComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Integerity = ent.Comp.MaxIntegrity;
    }

    private void OnGasEmitterInit(Entity<SupermatterGasEmitterComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.Delay;
        ent.Comp.CurrentRate = ent.Comp.MinRate;
        ent.Comp.CurrentTemperature = ent.Comp.MinTemperature;
    }

    private void OnAtmosExposed(Entity<SupermatterGasReactionComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (args.GasMixture.TotalMoles < Atmospherics.GasMinMoles)
        {
            foreach (var reaction in ent.Comp.SpaceReactions)
                reaction.React(ent, null, args.GasMixture, EntityManager);

            var spaceEv = new SupermatterSpaceGasReactedEvent();
            RaiseLocalEvent(ent, spaceEv);
            return;
        }

        Dictionary<Gas, HashSet<SupermatterGasReaction>> completedReactions = new();

        foreach (var (gas, reactions) in ent.Comp.GasReactions)
        {
            foreach (var reaction in reactions)
            {
                if (!reaction.React(ent, gas, args.GasMixture, EntityManager))
                    continue;

                if (completedReactions.TryGetValue(gas, out var newReactions))
                {
                    newReactions.Add(reaction);
                }
                else
                {
                    newReactions = new();
                    newReactions.Add(reaction);
                    completedReactions.Add(gas, newReactions);
                }
            }
        }

        var ev = new SupermatterGasReactedEvent(completedReactions);
        RaiseLocalEvent(ent, ev);
    }

    private void OnRadioIntegerityModified(Entity<SupermatterRadioComponent> ent, ref SupermatterIntegerityModifiedEvent args)
    {
        var positive = args.CurrentIntegerity - args.PreviousIntegerity > 0;
        KeyValuePair<FixedPoint2, LocId> match = new();

        foreach (var (threshold, message) in ent.Comp.Messages)
        {
            if (positive)
            {
                if (threshold > args.CurrentIntegerity)
                    continue;
            }
            else
            {
                if (threshold < args.CurrentIntegerity)
                    continue;
            }

            if (threshold > match.Key)
                match = new(threshold, message);
        }

        if (match.Key == ent.Comp.LastMessage)
            return;

        ent.Comp.LastMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value), ent.Comp.Channel, ent);
    }

    private void OnDelaminateableDelaminated(Entity<SupermatterDelaminatableComponent> ent, ref SupermatterDelaminatedEvent args)
    {
        foreach (var delamination in ent.Comp.Delaminations)
        {
            if (!delamination.RequirementsMet(ent, EntityManager))
                continue;

            delamination.Delaminate(ent, EntityManager);
            return;
        }
    }

    private void OnGasEmitterGasReact(Entity<SupermatterGasEmitterComponent> ent, ref SupermatterGasReactedEvent args)
    {
        // clear up any gases that we didnt react with
        foreach (var gas in ent.Comp.PreviousPercentage.Keys)
        {
            if (args.Reactions.TryGetValue(gas, out var reactions))
            {
                foreach (var reaction in reactions)
                {
                    if (ent.Comp.ModifiableReactions.Contains(reaction.GetType()))
                        continue;
                }
            }

            ent.Comp.PreviousPercentage.Remove(gas);
        }
    }

    private void OnGasEmitterSpaceReact(Entity<SupermatterGasEmitterComponent> ent, ref SupermatterSpaceGasReactedEvent args)
    {
        ent.Comp.PreviousPercentage.Clear();
    }

    public void ModifyIntegerity(Entity<SupermatterIntegerityComponent?> ent, FixedPoint2 integerity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldIntegerity = ent.Comp.Integerity;
        ent.Comp.Integerity += integerity;

        var modifiedEv = new SupermatterIntegerityModifiedEvent(ent.Comp.Integerity, oldIntegerity);
        RaiseLocalEvent(ent, modifiedEv);

        if (ent.Comp.Integerity < 0)
        {
            var delaminatingEv = new SupermatterDelaminatedEvent();
            RaiseLocalEvent(ent, delaminatingEv);
            ent.Comp.Integerity = 0f; //clamp it
        }
    }

    public void ModifyEnergy(Entity<SupermatterEnergyComponent?> ent, float energy)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldEnergy = ent.Comp.CurrentEnergy;
        ent.Comp.CurrentEnergy += energy;

        var modifiedEv = new SupermatterEnergyModifiedEvent(ent.Comp.CurrentEnergy, oldEnergy);
        RaiseLocalEvent(ent, modifiedEv);
    }
}
