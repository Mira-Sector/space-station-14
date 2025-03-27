using Content.Server.Radio.EntitySystems;
using Content.Server.Supermatter.Components;
using Content.Server.Supermatter.Events;
using Content.Server.Supermatter.GasReactions;
using Content.Server.Tesla.Components;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Radiation.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Events;

namespace Content.Server.Supermatter;

public sealed partial class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterIntegerityComponent, ComponentInit>(OnIntegerityInit);

        SubscribeLocalEvent<SupermatterDelaminatableComponent, SupermatterDelaminatedEvent>(OnDelaminateableDelaminated);

        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterIntegerityModifiedEvent>(OnRadioIntegerityModified);
        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterCountdownTickEvent>(OnRadioCountdownTick);
        SubscribeLocalEvent<SupermatterDelaminationCountdownComponent, SupermatterBeforeDelaminatedEvent>(OnCountdownBeforeDelamination);

        SubscribeLocalEvent<SupermatterGasReactionComponent, AtmosExposedUpdateEvent>(OnGasReactionAtmosExposed);

        SubscribeLocalEvent<SupermatterGasEmitterComponent, ComponentInit>(OnGasEmitterInit);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterGasReactedEvent>(OnGasEmitterGasReact);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterSpaceGasReactedEvent>(OnGasEmitterSpaceReact);

        SubscribeLocalEvent<SupermatterEnergyCollideComponent, StartCollideEvent>(OnEnergyCollideCollide);
        SubscribeLocalEvent<SupermatterModifyEnergyOnCollideComponent, SupermatterEnergyCollidedEvent>(OnModifyEnergyCollide);

        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, ComponentInit>(OnArcShooterInit);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, AtmosExposedUpdateEvent>(OnArcShooterAtmosExposed);

        SubscribeLocalEvent<SupermatterRadiationComponent, ComponentInit>(OnRadiationInit);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterEnergyModifiedEvent>(OnRadiationEnergyModified);

        SubscribeLocalEvent<SupermatterEnergyDecayComponent, AtmosExposedUpdateEvent>(OnDecayAtmosExposed);
        SubscribeLocalEvent<SupermatterEnergyHeatGainComponent, AtmosExposedUpdateEvent>(OnHeatGainAtmosExposed);
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

            var air = _atmos.GetContainingMixture(uid, false, false);

            if (air == null)
                continue;

            foreach (var (gas, ratio) in gasEmitterComp.Ratios)
                air.AdjustMoles(gas, ratio * gasEmitterComp.CurrentRate);

            air.Temperature += gasEmitterComp.CurrentTemperature;
        }

        var countdownQuery = EntityQueryEnumerator<SupermatterDelaminationCountdownComponent>();
        while (countdownQuery.MoveNext(out var uid, out var countdownComp))
        {
            if (!countdownComp.Active)
                continue;

            countdownComp.ElapsedTime += TimeSpan.FromSeconds(frameTime);

            if (countdownComp.ElapsedTime > countdownComp.Length)
            {
                var delaminatingEv = new SupermatterDelaminatedEvent();
                RaiseLocalEvent(uid, delaminatingEv);
                countdownComp.Active = false;
                continue;
            }

            // so we dont spam events
            if (countdownComp.NextTick > _timing.CurTime)
                continue;

            var timerEv = new SupermatterCountdownTickEvent(countdownComp.ElapsedTime);
            RaiseLocalEvent(uid, timerEv);
            countdownComp.NextTick += countdownComp.TickDelay;
        }

        var decayQuery = EntityQueryEnumerator<SupermatterEnergyDecayComponent, SupermatterEnergyComponent>();
        while (decayQuery.MoveNext(out var uid, out var decayComp, out var energyComp))
        {
            if (decayComp.NextDecay > _timing.CurTime)
                continue;

            decayComp.NextDecay += decayComp.Delay;

            ModifyEnergy((uid, energyComp), decayComp.EnergyDecay);
            decayComp.LastLostEnergy += decayComp.EnergyDecay;
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

    private void OnArcShooterInit(Entity<SupermatterEnergyArcShooterComponent> ent, ref ComponentInit args)
    {
        EnsureComp<LightningArcShooterComponent>(ent, out var arcShooterComp);
        ent.Comp.MinInterval = arcShooterComp.ShootMinInterval;
        ent.Comp.MaxInterval = arcShooterComp.ShootMaxInterval;
    }

    private void OnRadiationInit(Entity<SupermatterRadiationComponent> ent, ref ComponentInit args)
    {
        EnsureComp<RadiationSourceComponent>(ent, out var radiationComp);
        ent.Comp.Intensity = radiationComp.Intensity;
        ent.Comp.Slope = radiationComp.Slope;
    }

    private void OnGasReactionAtmosExposed(Entity<SupermatterGasReactionComponent> ent, ref AtmosExposedUpdateEvent args)
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
        var match = GetRadioMessage<FixedPoint2>(ent.Comp.IntegerityMessages, args.CurrentIntegerity, positive);
        ent.Comp.LastIntegerityMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value, ("key", match.Key)), ent.Comp.Channel, ent);
    }

    private void OnRadioCountdownTick(Entity<SupermatterRadioComponent> ent, ref SupermatterCountdownTickEvent args)
    {
        var match = GetRadioMessage<TimeSpan>(ent.Comp.CountdownMessages, args.ElapsedTime, true);
        ent.Comp.LastCountdownMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value, ("key", match.Key)), ent.Comp.Channel, ent);
    }

    private KeyValuePair<T, LocId> GetRadioMessage<T>(Dictionary<T, LocId> messages, T comparison, bool positive) where T : IComparable<T>
    {
        KeyValuePair<T, LocId> match = new();

        foreach (var (key, message) in messages)
        {
            if (positive)
            {
                if (key.CompareTo(comparison) > 0)
                    continue;
            }
            else
            {
                if (key.CompareTo(comparison) < 0)
                    continue;
            }

            if (key.CompareTo(match.Key) > 0)
                match = new(key, message);
        }

        return match;
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

    private void OnCountdownBeforeDelamination(Entity<SupermatterDelaminationCountdownComponent> ent, ref SupermatterBeforeDelaminatedEvent args)
    {
        args.Handled = true;
        ent.Comp.ElapsedTime = TimeSpan.Zero;
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

    private void OnEnergyCollideCollide(Entity<SupermatterEnergyCollideComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.OurFixtureId || args.OtherFixtureId != ent.Comp.OtherFixtureId)
            return;

        if (!_whitelist.CheckBoth(args.OtherEntity, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        var ev = new SupermatterEnergyCollidedEvent(ent, ent.Comp.BaseEnergyOnCollide);
        RaiseLocalEvent(args.OtherEntity, ev);

        EntityManager.DeleteEntity(args.OtherEntity);
        ModifyEnergy(ent.Owner, ev.Energy);
    }

    private void OnModifyEnergyCollide(Entity<SupermatterModifyEnergyOnCollideComponent> ent, ref SupermatterEnergyCollidedEvent args)
    {
        args.Energy *= ent.Comp.Scale;
        args.Energy += ent.Comp.Additional;
    }

    private void OnArcShooterAtmosExposed(Entity<SupermatterEnergyArcShooterComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        var arcComp = EntityManager.GetComponent<LightningArcShooterComponent>(ent);
        arcComp.ShootMinInterval = ent.Comp.MinInterval;
        arcComp.ShootMaxInterval = ent.Comp.MaxInterval;
    }

    private void OnRadiationEnergyModified(Entity<SupermatterRadiationComponent> ent, ref SupermatterEnergyModifiedEvent args)
    {
        var radiationComp = EntityManager.GetComponent<RadiationSourceComponent>(ent);
        radiationComp.Intensity = ent.Comp.Intensity + (float) Math.Log(args.CurrentEnergy, ent.Comp.IntensityBase);
        radiationComp.Slope = ent.Comp.Slope - (float) Math.Log(args.CurrentEnergy, ent.Comp.SlopeBase);
    }

    private void OnDecayAtmosExposed(Entity<SupermatterEnergyDecayComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        ent.Comp.LastLostEnergy = 0f;
    }

    private void OnHeatGainAtmosExposed(Entity<SupermatterEnergyHeatGainComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        ent.Comp.CurrentGain = 0f;
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
            ent.Comp.Integerity = 0f; //clamp it

            var beforeDelaminateEv = new SupermatterBeforeDelaminatedEvent();
            RaiseLocalEvent(ent, beforeDelaminateEv);

            if (beforeDelaminateEv.Handled)
                return;

            var delaminatingEv = new SupermatterDelaminatedEvent();
            RaiseLocalEvent(ent, delaminatingEv);
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
