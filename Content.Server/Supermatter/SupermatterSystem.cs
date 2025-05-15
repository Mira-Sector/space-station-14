using Content.Server.Radio.EntitySystems;
using Content.Server.Singularity.Events;
using Content.Server.Supermatter.Components;
using Content.Server.Supermatter.Delaminations;
using Content.Server.Supermatter.Events;
using Content.Server.Supermatter.GasReactions;
using Content.Server.Tesla.Components;
using Content.Shared.Audio;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Radiation.Components;
using Content.Shared.Supermatter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Supermatter;

public sealed partial class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterActiveComponent, MapInitEvent>(OnActiveInit);
        SubscribeLocalEvent<SupermatterActiveComponent, EntityConsumedByEventHorizonEvent>(OnActiveEventHorizon);

        SubscribeLocalEvent<SupermatterIntegerityComponent, ComponentInit>(OnIntegerityInit);
        SubscribeLocalEvent<SupermatterSpawnOnIntegerityComponent, SupermatterActivatedEvent>(OnSpawnIntegerityActivated);
        SubscribeLocalEvent<SupermatterSpawnOnIntegerityComponent, SupermatterDeactivatedEvent>(OnSpawnIntegerityDeactivated);
        SubscribeLocalEvent<SupermatterSpawnOnIntegerityComponent, SupermatterIntegerityModifiedEvent>(OnSpawnIntegerityModified);

        SubscribeLocalEvent<SupermatterPowerTransmissionComponent, ComponentInit>(OnPowerInit);
        SubscribeLocalEvent<SupermatterPowerTransmissionComponent, SupermatterBeforeGasReactionsEvent>(OnPowerBeforeGasReaction);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, ComponentInit>(OnArcShooterInit);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, SupermatterActivatedEvent>(OnArcShooterActivated);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, SupermatterDeactivatedEvent>(OnArcShooterDeactivated);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, SupermatterPowerTransmissionModifiedEvent>(OnArcShooterPowerModified);

        SubscribeLocalEvent<SupermatterHeatResistanceComponent, SupermatterBeforeGasReactionsEvent>(OnHeatResistanceBeforeGasReaction);
        SubscribeLocalEvent<SupermatterModifyIntegerityOnHeatResistanceComponent, SupermatterActivatedEvent>(OnHeatResistanceIntegerityActivated);
        SubscribeLocalEvent<SupermatterModifyIntegerityOnHeatResistanceComponent, AtmosExposedUpdateEvent>(OnHeatResistanceIntegerityAtmosExposed);

        SubscribeLocalEvent<SupermatterGasReactionComponent, SupermatterActivatedEvent>(OnGasReactionActivated);
        SubscribeLocalEvent<SupermatterGasReactionComponent, AtmosExposedUpdateEvent>(OnGasReactionAtmosExposed);
        SubscribeLocalEvent<SupermatterGasAbsorberComponent, SupermatterGasReactedEvent>(OnGasAbsorberGasReacted);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, ComponentInit>(OnGasEmitterInit);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterBeforeGasReactionsEvent>(OnGasEmitterBeforeGasReaction);

        SubscribeLocalEvent<SupermatterHeatEnergyComponent, ComponentInit>(OnHeatEnergyInit);
        SubscribeLocalEvent<SupermatterHeatEnergyComponent, SupermatterBeforeGasReactionsEvent>(OnHeatEnergyBeforeGasReaction);
        SubscribeLocalEvent<SupermatterHeatEnergyComponent, SupermatterGasReactedEvent>(OnHeatEnergyGasReacted);

        SubscribeLocalEvent<SupermatterDelaminatableComponent, SupermatterDelaminatedEvent>(OnDelaminateableDelaminated);
        SubscribeLocalEvent<SupermatterDelaminationCountdownComponent, SupermatterBeforeDelaminatedEvent>(OnCountdownBeforeDelamination);
        SubscribeLocalEvent<SupermatterDelaminationTeleportMapComponent, SupermatterDelaminationTeleportGetPositionEvent>(OnDelaminationTeleportGetPos);
        SubscribeLocalEvent<SupermatterDelaminationTeleportMapReturnComponent, ComponentInit>(OnMapReturnInit);

        SubscribeLocalEvent<SupermatterRadioComponent, ComponentInit>(OnRadioInit);
        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterIntegerityModifiedEvent>(OnRadioIntegerityModified);
        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterCountdownTickEvent>(OnRadioCountdownTick);

        SubscribeLocalEvent<SupermatterEnergyCollideComponent, StartCollideEvent>(OnEnergyCollideCollide);
        SubscribeLocalEvent<SupermatterModifyEnergyOnCollideComponent, SupermatterEnergyCollidedEvent>(OnModifyEnergyCollide);
        SubscribeLocalEvent<SupermatterEnergyDecayComponent, SupermatterBeforeGasReactionsEvent>(OnDecayBeforeGasReaction);
        SubscribeLocalEvent<SupermatterModifyIntegerityOnEnergyComponent, SupermatterEnergyModifiedEvent>(OnEnergyIntegerityModifyEnergy);

        SubscribeLocalEvent<SupermatterRadiationComponent, ComponentInit>(OnRadiationInit);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterActivatedEvent>(OnRadiationActivated);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterDeactivatedEvent>(OnRadiationDeactivated);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterEnergyModifiedEvent>(OnRadiationEnergyModified);

        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterActivatedEvent>(OnAudioActivated);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterDeactivatedEvent>(OnAudioDeactivated);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterBeforeDelaminatedEvent>(OnAudioBeforeDelamination);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterDelaminatedEvent>(OnAudioDelaminated);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterIntegerityModifiedEvent>(OnAudioIntegerityModified);

        SubscribeLocalEvent<SupermatterModifyIntegerityOnMolesComponent, SupermatterGasAbsorbedEvent>(OnMolesIntegerityGasAbsorbed);
        SubscribeLocalEvent<SupermatterModifyHeatResistanceOnMolesComponent, SupermatterGasAbsorbedEvent>(OnMolesHeatResistanceGasAbsorbed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

#region Gas Emitter

        var gasEmitterQuery = EntityQueryEnumerator<SupermatterGasEmitterComponent>();
        while (gasEmitterQuery.MoveNext(out var uid, out var gasEmitterComp))
        {
            if (!IsActive(uid))
                continue;

            if (gasEmitterComp.NextSpawn > _timing.CurTime)
                continue;

            gasEmitterComp.NextSpawn += gasEmitterComp.Delay;

            var air = _atmos.GetContainingMixture(uid, false, false);

            if (air == null)
                continue;

            foreach (var (gas, ratio) in gasEmitterComp.Ratios)
                air.AdjustMoles(gas, ratio * gasEmitterComp.CurrentRate);

            air.Temperature -= gasEmitterComp.LastTemperature;
            var temp = gasEmitterComp.CurrentRate * gasEmitterComp.TemperaturePerRate + Math.Max(gasEmitterComp.MinTemperature - air.Temperature, 0f);
            gasEmitterComp.LastTemperature = temp;
            air.Temperature += temp;
        }

#endregion

#region Delamination

        var countdownQuery = EntityQueryEnumerator<SupermatterDelaminationCountdownComponent>();
        while (countdownQuery.MoveNext(out var uid, out var countdownComp))
        {
            if (!countdownComp.Active || !IsActive(uid))
                continue;

            countdownComp.ElapsedTime += TimeSpan.FromSeconds(frameTime);

            if (countdownComp.ElapsedTime >= countdownComp.Length)
            {
                var delaminatingEv = new SupermatterDelaminatedEvent();
                RaiseLocalEvent(uid, delaminatingEv);
                countdownComp.Active = false;
                continue;
            }

            // so we dont spam events
            if (countdownComp.NextTick > _timing.CurTime)
                continue;

            var timerEv = new SupermatterCountdownTickEvent(countdownComp.ElapsedTime, countdownComp.Length);
            RaiseLocalEvent(uid, timerEv);
            countdownComp.NextTick += countdownComp.TickDelay;
        }

#endregion

        var decayQuery = EntityQueryEnumerator<SupermatterEnergyDecayComponent, SupermatterEnergyComponent>();
        while (decayQuery.MoveNext(out var uid, out var decayComp, out var energyComp))
        {
            if (decayComp.NextDecay > _timing.CurTime)
                continue;

            decayComp.NextDecay += decayComp.Delay;

            var decay = ((float) Math.Pow((1 + decayComp.DecayPower), energyComp.CurrentEnergy) + decayComp.DecayOffset - 1) * -1;

            ModifyEnergy((uid, energyComp), decay);
            decayComp.LastLostEnergy += decay;
        }


        var integeritySpawnQuery = EntityQueryEnumerator<SupermatterSpawnOnIntegerityComponent>();
        while (integeritySpawnQuery.MoveNext(out var uid, out var spawnComp))
        {
            if (IsActive(uid))
                continue;

            var pos = _transform.GetMapCoordinates(uid);

            foreach (var spawn in spawnComp.Spawns)
            {
                if (spawn.CanSpawn)
                    continue;

                if (spawn.NextSpawn > _timing.CurTime)
                    continue;

                spawn.NextSpawn += _random.Next(spawn.MinDelay, spawn.MaxDelay);

                for (var i = 0; i < _random.Next(spawn.MinSpawns, spawn.MaxSpawns); i++)
                {
                    var newPos = spawn.Range == null ? pos : new MapCoordinates(pos.Position + _random.NextVector2(-spawn.Range.Value, spawn.Range.Value), pos.MapId);
                    Spawn(_random.Pick(spawn.Prototypes), newPos);
                }
            }
        }

#region Audio

        var audioQuery = EntityQueryEnumerator<SupermatterAudioComponent>();
        while (audioQuery.MoveNext(out var uid, out var audioComp))
        {
            if (!IsActive(uid))
                continue;

            if (audioComp.NextPulseSound > _timing.CurTime)
                continue;

            audioComp.NextPulseSound += _random.Next(audioComp.MinPulseSoundDelay, audioComp.MaxPulseSoundDelay);

            SoundSpecifier pulseSound;
            if (audioComp.DelaminationSounds)
            {
                if (audioComp.DelaminationPulse == null)
                    return;

                pulseSound = audioComp.DelaminationPulse;
            }
            else
            {
                if (audioComp.NormalPulse == null)
                    return;

                pulseSound = audioComp.NormalPulse;
            }

            _audio.PlayEntity(pulseSound, Filter.Pvs(uid), uid, true);
        }

#endregion

#region Delamination

        var mapReturnQuery = EntityQueryEnumerator<SupermatterDelaminationTeleportMapReturnComponent>();
        while (mapReturnQuery.MoveNext(out var uid, out var mapReturnComp))
        {
            if (mapReturnComp.NextTeleport > _timing.CurTime)
                continue;

            var grids = Transform(uid).ChildEnumerator;
            while (grids.MoveNext(out var grid))
            {
                var children = Transform(grid).ChildEnumerator;
                while (children.MoveNext(out var entity))
                {
                    if (TryComp<SupermatterDelaminationTeleportedComponent>(entity, out var teleportedComp))
                    {
                        _transform.SetMapCoordinates(entity, teleportedComp.StartingCoords);
                        RemCompDeferred<SupermatterDelaminationTeleportedComponent>(entity);
                    }
                }
            }

            Del(uid);
        }

#endregion

    }

#region Active

    private void OnActiveInit(Entity<SupermatterActiveComponent> ent, ref MapInitEvent args)
    {
        ActivateSupermatter((ent, ent.Comp), ent.Comp.Active);
    }

    private void OnActiveEventHorizon(Entity<SupermatterActiveComponent> ent, ref EntityConsumedByEventHorizonEvent args)
    {
        if (_whitelist.IsBlacklistPass(ent.Comp.ActivationBlackList, args.Entity))
            return;

        ActivateSupermatter((ent, ent.Comp), true);
    }

#endregion

#region Integerity

    private static void OnIntegerityInit(Entity<SupermatterIntegerityComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Integerity = ent.Comp.MaxIntegrity;
    }

    private void OnSpawnIntegerityActivated(Entity<SupermatterSpawnOnIntegerityComponent> ent, ref SupermatterActivatedEvent args)
    {
        SpawnIntegerityCheckSpawns(ent);
    }

    private static void OnSpawnIntegerityDeactivated(Entity<SupermatterSpawnOnIntegerityComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        foreach (var spawn in ent.Comp.Spawns)
            spawn.CanSpawn = false;
    }

    private void OnSpawnIntegerityModified(Entity<SupermatterSpawnOnIntegerityComponent> ent, ref SupermatterIntegerityModifiedEvent args)
    {
        SpawnIntegerityCheckSpawns(ent, args.CurrentIntegerity);
    }

    private void SpawnIntegerityCheckSpawns(Entity<SupermatterSpawnOnIntegerityComponent> ent, FixedPoint2? integerity = null)
    {
        if (integerity == null)
        {
            if (!TryComp<SupermatterIntegerityComponent>(ent, out var integerityComp))
                return;

            integerity = integerityComp.Integerity;
        }

        foreach (var spawn in ent.Comp.Spawns)
        {
            var oldCanSpawn = spawn.CanSpawn;
            spawn.CanSpawn = spawn.Min < integerity && spawn.Max >= integerity;

            if (!oldCanSpawn && spawn.CanSpawn)
                spawn.NextSpawn = _timing.CurTime + _random.Next(spawn.MinDelay, spawn.MaxDelay);
        }
    }

#endregion

#region Power Transmission

    private static void OnPowerInit(Entity<SupermatterPowerTransmissionComponent> ent, ref ComponentInit args)
    {
        ent.Comp.CurrentPower = ent.Comp.BasePower;
    }

    private static void OnPowerBeforeGasReaction(Entity<SupermatterPowerTransmissionComponent> ent, ref SupermatterBeforeGasReactionsEvent args) => ent.Comp.CurrentPower = ent.Comp.BasePower;

    private void OnArcShooterInit(Entity<SupermatterEnergyArcShooterComponent> ent, ref ComponentInit args)
    {
        EnsureComp<LightningArcShooterComponent>(ent, out var arcShooterComp);
        ent.Comp.Arcs = arcShooterComp.MaxLightningArc;
        ent.Comp.MinDelay = arcShooterComp.ShootMinInterval;
        ent.Comp.MaxDelay = arcShooterComp.ShootMaxInterval;
        arcShooterComp.Enabled = false;
    }

    private void OnArcShooterActivated(Entity<SupermatterEnergyArcShooterComponent> ent, ref SupermatterActivatedEvent args)
    {
        var arcShooterComp = EntityManager.GetComponent<LightningArcShooterComponent>(ent);
        arcShooterComp.Enabled = true;
        arcShooterComp.NextShootTime = _timing.CurTime;
    }

    private void OnArcShooterDeactivated(Entity<SupermatterEnergyArcShooterComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        EntityManager.GetComponent<LightningArcShooterComponent>(ent).Enabled = false;
    }

    private void OnArcShooterPowerModified(Entity<SupermatterEnergyArcShooterComponent> ent, ref SupermatterPowerTransmissionModifiedEvent args)
    {
        var arcComp = EntityManager.GetComponent<LightningArcShooterComponent>(ent);

        var delta = args.CurrentPower - args.PreviousPower;
        var delayScaled = delta * ent.Comp.DelayScale;

        arcComp.MaxLightningArc += (int) Math.Max(Math.Floor(delta / ent.Comp.EnergyRequiredForArc), 0);
        arcComp.ShootMinInterval = ent.Comp.MinDelay / delayScaled;
        arcComp.ShootMaxInterval = ent.Comp.MaxDelay / delayScaled;
    }

#endregion

#region Heat Resistance

    private static void OnHeatResistanceBeforeGasReaction(Entity<SupermatterHeatResistanceComponent> ent, ref SupermatterBeforeGasReactionsEvent args) => ent.Comp.HeatResistance = ent.Comp.BaseHeatResistance;

    private void OnHeatResistanceIntegerityActivated(Entity<SupermatterModifyIntegerityOnHeatResistanceComponent> ent, ref SupermatterActivatedEvent args)
    {
        ent.Comp.LastReaction = _timing.CurTime;
    }

    private void OnHeatResistanceIntegerityAtmosExposed(Entity<SupermatterModifyIntegerityOnHeatResistanceComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (!IsActive(ent.Owner))
            return;

        if (!TryComp<SupermatterHeatResistanceComponent>(ent, out var heatResistanceComp))
            return;

        var lastReaction = _timing.CurTime - ent.Comp.LastReaction;

        foreach (var damages in ent.Comp.Damages)
        {
            if (damages.BelowHeatResistance)
            {
                if (args.GasMixture.Temperature < heatResistanceComp.HeatResistance)
                    continue;
            }
            else
            {
                if (args.GasMixture.Temperature > heatResistanceComp.HeatResistance)
                    continue;
            }

            ModifyIntegerity(ent.Owner, damages.IntegerityDamage * lastReaction.TotalSeconds);
        }

        ent.Comp.LastReaction = _timing.CurTime;
    }

#endregion

#region Gas

    private void OnGasReactionActivated(Entity<SupermatterGasReactionComponent> ent, ref SupermatterActivatedEvent args)
    {
        ent.Comp.LastReaction = _timing.CurTime;
    }

    private void OnGasReactionAtmosExposed(Entity<SupermatterGasReactionComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (!IsActive(ent.Owner))
            return;

        var beforeEv = new SupermatterBeforeGasReactionsEvent();
        RaiseLocalEvent(ent, beforeEv);

        var lastReaction = _timing.CurTime - ent.Comp.LastReaction;

        if (args.GasMixture.TotalMoles < Atmospherics.GasMinMoles)
        {
            foreach (var reaction in ent.Comp.SpaceReactions)
                reaction.React(ent, null, args.GasMixture, lastReaction, EntityManager);

            ent.Comp.LastReaction = _timing.CurTime;

            var spaceEv = new SupermatterSpaceGasReactedEvent();
            RaiseLocalEvent(ent, spaceEv);
            return;
        }

        Dictionary<Gas, HashSet<SupermatterGasReaction>> completedReactions = new();

        foreach (var (gas, reactions) in ent.Comp.GasReactions)
        {
            if (args.GasMixture.GetMoles(gas) < Atmospherics.GasMinMoles)
                continue;

            foreach (var reaction in reactions)
            {
                if (!reaction.React(ent, gas, args.GasMixture, lastReaction, EntityManager))
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

        var ev = new SupermatterGasReactedEvent(completedReactions, lastReaction);
        RaiseLocalEvent(ent, ev);

        ent.Comp.LastReaction = _timing.CurTime;
    }

    private void OnGasAbsorberGasReacted(Entity<SupermatterGasAbsorberComponent> ent, ref SupermatterGasReactedEvent args)
    {
        ent.Comp.AbsorbedMoles.Clear();
        ent.Comp.TotalMoles = 0f;

        var air = _atmos.GetContainingMixture(ent.Owner, false, false);

        if (air == null)
            return;

        Dictionary<Gas, float> absorbedMoles = new();
        float totalMoles = 0f;

        foreach (var gas in args.Reactions.Keys)
        {
            var molePercentage = air.GetMoles(gas) / air.TotalMoles;
            var newMoles = molePercentage * ent.Comp.AbsorbedMultiplier * (float) args.LastReaction.TotalSeconds;
            air.AdjustMoles(gas, -newMoles);

            absorbedMoles.Add(gas, newMoles);
            totalMoles += newMoles;
        }

        ent.Comp.AbsorbedMoles = absorbedMoles;
        ent.Comp.TotalMoles = totalMoles;

        var ev = new SupermatterGasAbsorbedEvent(absorbedMoles, totalMoles, args.LastReaction);
        RaiseLocalEvent(ent, ev);
    }

    private void OnGasEmitterInit(Entity<SupermatterGasEmitterComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.Delay;
        ent.Comp.CurrentRate = ent.Comp.BaseRate;
    }

    private static void OnGasEmitterBeforeGasReaction(Entity<SupermatterGasEmitterComponent> ent, ref SupermatterBeforeGasReactionsEvent args) => ent.Comp.CurrentRate = ent.Comp.BaseRate;

#endregion

#region Heat Energy

    private static void OnHeatEnergyInit(Entity<SupermatterHeatEnergyComponent> ent, ref ComponentInit args) => ent.Comp.CurrentEnergy = 0f;

    private static void OnHeatEnergyBeforeGasReaction(Entity<SupermatterHeatEnergyComponent> ent, ref SupermatterBeforeGasReactionsEvent args) => ent.Comp.CurrentEnergy = 0f;

    private void OnHeatEnergyGasReacted(Entity<SupermatterHeatEnergyComponent> ent, ref SupermatterGasReactedEvent args)
    {
        if (ent.Comp.CurrentEnergy <= 0f)
            return;

        var air = _atmos.GetContainingMixture(ent.Owner, false, false);

        if (air == null)
            return;

        var delta = air.Temperature - ent.Comp.MinTemperature;

        if (delta <= 0f)
            return;

        var energy = (delta * ent.Comp.SaturationTemperature) * ent.Comp.CurrentEnergy * (float) args.LastReaction.TotalSeconds;

        ModifyEnergy(ent.Owner, energy);
    }

#endregion

#region Delamination

    private void OnDelaminateableDelaminated(Entity<SupermatterDelaminatableComponent> ent, ref SupermatterDelaminatedEvent args)
    {
        foreach (var data in ent.Comp.Delaminations)
        {
            if (!RequirementsMet(data.Requirements))
                continue;

            foreach (var delamination in data.Delaminations)
                delamination.Delaminate(ent, EntityManager);

            return;
        }

        bool RequirementsMet(HashSet<DelaminationRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (!requirement.RequirementMet(ent, EntityManager))
                    return false;
            }

            return true;
        }
    }

    private static void OnCountdownBeforeDelamination(Entity<SupermatterDelaminationCountdownComponent> ent, ref SupermatterBeforeDelaminatedEvent args)
    {
        args.Handled = true;
        ent.Comp.ElapsedTime = TimeSpan.Zero;
        ent.Comp.Active = true;
    }

    private void OnDelaminationTeleportGetPos(Entity<SupermatterDelaminationTeleportMapComponent> ent, ref SupermatterDelaminationTeleportGetPositionEvent args)
    {
        foreach (var (entity, pos) in args.Entities)
        {
            if (!_mapLoader.TryLoadMap(ent.Comp.MapPath, out var map, out _))
                continue;

            _map.InitializeMap(map.Value.Comp.MapId);
            _map.SetPaused(map.Value.Comp.MapId, false);
            args.Entities[entity] = new MapCoordinates(ent.Comp.MapPosition, map.Value.Comp.MapId);
        }

        args.Handled = true;
    }

    private void OnMapReturnInit(Entity<SupermatterDelaminationTeleportMapReturnComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextTeleport = _timing.CurTime + ent.Comp.Delay;
    }

#endregion

#region Radio

    private void OnRadioInit(Entity<SupermatterRadioComponent> ent, ref ComponentInit args)
    {
        if (TryComp<SupermatterIntegerityComponent>(ent, out var integerityComp))
            ent.Comp.LastIntegerityMessage = 100f; //dont at me

        if (TryComp<SupermatterDelaminationCountdownComponent>(ent, out var countdownComp))
            ent.Comp.LastCountdownMessage = countdownComp.Length;
    }

    private void OnRadioIntegerityModified(Entity<SupermatterRadioComponent> ent, ref SupermatterIntegerityModifiedEvent args)
    {
        var positive = args.CurrentIntegerity - args.PreviousIntegerity > 0;
        var match = GetRadioMessage<FixedPoint2>(ent.Comp.IntegerityMessages, (args.CurrentIntegerity / args.MaxIntegerity) * 100, positive);

        if (match.Key == ent.Comp.LastIntegerityMessage)
            return;

        ent.Comp.LastIntegerityMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value, ("key", match.Key)), ent.Comp.Channel, ent);
    }

    private void OnRadioCountdownTick(Entity<SupermatterRadioComponent> ent, ref SupermatterCountdownTickEvent args)
    {
        var match = GetRadioMessage<TimeSpan>(ent.Comp.CountdownMessages, args.Timer - args.ElapsedTime, false);

        if (match.Key == ent.Comp.LastCountdownMessage)
            return;

        ent.Comp.LastCountdownMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value, ("key", match.Key.TotalSeconds)), ent.Comp.Channel, ent);
    }

    internal static KeyValuePair<T, LocId> GetRadioMessage<T>(SortedDictionary<T, LocId> messages, T comparison, bool positive) where T : IComparable<T>
    {
        KeyValuePair<T, LocId> match = new();

        foreach (var (key, message) in messages)
        {
            if (positive)
            {
                if (key.CompareTo(comparison) < 0)
                    continue;
            }
            else
            {
                if (key.CompareTo(comparison) > 0)
                    continue;
            }

            if (key.CompareTo(match.Key) > 0)
                match = new(key, message);
        }

        return match;
    }

#endregion

#region Energy

    private void OnEnergyCollideCollide(Entity<SupermatterEnergyCollideComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.OurFixtureId)
            return;

        if (!_whitelist.CheckBoth(args.OtherEntity, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        var ev = new SupermatterEnergyCollidedEvent(ent, ent.Comp.BaseEnergyOnCollide);
        RaiseLocalEvent(args.OtherEntity, ev);

        ModifyEnergy(ent.Owner, ev.Energy);

        QueueDel(args.OtherEntity);
    }

    private static void OnModifyEnergyCollide(Entity<SupermatterModifyEnergyOnCollideComponent> ent, ref SupermatterEnergyCollidedEvent args)
    {
        args.Energy *= ent.Comp.Scale;
        args.Energy += ent.Comp.Additional;
    }

    private static void OnDecayBeforeGasReaction(Entity<SupermatterEnergyDecayComponent> ent, ref SupermatterBeforeGasReactionsEvent args) => ent.Comp.LastLostEnergy = 0f;

    private void OnEnergyIntegerityModifyEnergy(Entity<SupermatterModifyIntegerityOnEnergyComponent> ent, ref SupermatterEnergyModifiedEvent args)
    {
        if (args.CurrentEnergy > ent.Comp.Max || args.CurrentEnergy < ent.Comp.Min)
            return;

        var delta = args.CurrentEnergy - args.PreviousEnergy;

        if (ent.Comp.IntegerityPerEnergy < 0)
        {
            // dont change the integerity if we are recovering
            if (delta < 0)
                return;
        }
        else
        {
            // dont change the intergerity if we are loosing energy
            if (delta > 0)
                return;
        }

        ModifyIntegerity(ent.Owner, ent.Comp.IntegerityPerEnergy * delta);
    }

#endregion

#region Radiation

    private void OnRadiationInit(Entity<SupermatterRadiationComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Intensity = EnsureComp<RadiationSourceComponent>(ent).Intensity;
    }

    private void OnRadiationActivated(Entity<SupermatterRadiationComponent> ent, ref SupermatterActivatedEvent args)
    {
        EntityManager.GetComponent<RadiationSourceComponent>(ent).Enabled = true;
    }

    private void OnRadiationDeactivated(Entity<SupermatterRadiationComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        EntityManager.GetComponent<RadiationSourceComponent>(ent).Enabled = false;
    }

    private void OnRadiationEnergyModified(Entity<SupermatterRadiationComponent> ent, ref SupermatterEnergyModifiedEvent args)
    {
        EntityManager.GetComponent<RadiationSourceComponent>(ent).Intensity = ent.Comp.Intensity + (float) Math.Pow((1 + ent.Comp.IntensityPower), args.CurrentEnergy);
    }

#endregion

#region Audio

    private void OnAudioActivated(Entity<SupermatterAudioComponent> ent, ref SupermatterActivatedEvent args)
    {
        _ambientSound.SetAmbience(ent, true);
        ent.Comp.NextPulseSound = _timing.CurTime;
    }

    private void OnAudioDeactivated(Entity<SupermatterAudioComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        _ambientSound.SetAmbience(ent, false);
    }

    private void OnAudioBeforeDelamination(Entity<SupermatterAudioComponent> ent, ref SupermatterBeforeDelaminatedEvent args) => AudioDelaminationSounds(ent);
    private void OnAudioDelaminated(Entity<SupermatterAudioComponent> ent, ref SupermatterDelaminatedEvent args) => AudioDelaminationSounds(ent);

    private void AudioDelaminationSounds(Entity<SupermatterAudioComponent> ent)
    {
        // dirtying is expensive so dont do it twice
        if (ent.Comp.DelaminationSounds)
            return;

        ent.Comp.DelaminationSounds = true;

        if (ent.Comp.DelaminationLoop == null)
        {
            _ambientSound.SetAmbience(ent, false);
            return;
        }

        _ambientSound.SetSound(ent, ent.Comp.DelaminationLoop);
        _ambientSound.SetAmbience(ent, true);
    }

    private void OnAudioIntegerityModified(Entity<SupermatterAudioComponent> ent, ref SupermatterIntegerityModifiedEvent args)
    {
        if (!ent.Comp.DelaminationSounds)
            return;

        ent.Comp.DelaminationSounds = false;

        if (ent.Comp.NormalLoop == null)
        {
            _ambientSound.SetAmbience(ent, false);
            return;
        }

        _ambientSound.SetSound(ent, ent.Comp.NormalLoop);
        _ambientSound.SetAmbience(ent, true);
    }

#endregion

#region Moles

    private void OnMolesIntegerityGasAbsorbed(Entity<SupermatterModifyIntegerityOnMolesComponent> ent, ref SupermatterGasAbsorbedEvent args)
    {
        foreach (var damages in ent.Comp.Damages)
        {
            if (damages.MinMoles > args.TotalMoles || damages.MaxMoles < args.TotalMoles)
                continue;

            ModifyIntegerity(ent.Owner, damages.IntegerityDamage * (float) args.LastReaction.TotalSeconds);
        }
    }

    private void OnMolesHeatResistanceGasAbsorbed(Entity<SupermatterModifyHeatResistanceOnMolesComponent> ent, ref SupermatterGasAbsorbedEvent args)
    {
        if (!TryComp<SupermatterHeatResistanceComponent>(ent, out var heatResistanceComp))
            return;

        foreach (var resistances in ent.Comp.Resistances)
        {
            if (resistances.MinMoles > args.TotalMoles || resistances.MaxMoles < args.TotalMoles)
                continue;

            ModifyHeatResistance((ent.Owner, heatResistanceComp), heatResistanceComp.BaseHeatResistance * resistances.HeatResistance * (float) args.LastReaction.TotalSeconds);
        }
    }

#endregion

#region Public Methods

    public void ModifyIntegerity(Entity<SupermatterIntegerityComponent?> ent, FixedPoint2 integerity)
    {
        if (integerity == FixedPoint2.Zero)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (integerity > 0 && ent.Comp.Integerity == ent.Comp.MaxIntegrity)
            return;

        var oldIntegerity = ent.Comp.Integerity;
        var newIntegerity = ent.Comp.Integerity + integerity;

        if (newIntegerity > 0)
        {
            var modifiedEv = new SupermatterIntegerityModifiedEvent(newIntegerity, oldIntegerity, ent.Comp.MaxIntegrity);
            RaiseLocalEvent(ent, modifiedEv);

            ent.Comp.Integerity = newIntegerity > ent.Comp.MaxIntegrity ? ent.Comp.MaxIntegrity : newIntegerity;
            ent.Comp.IsDelaminating = false;
            _appearance.SetData(ent, SupermatterVisuals.Delaminating, false);
            return;
        }

        if (ent.Comp.IsDelaminating)
            return;

        ent.Comp.IsDelaminating = true;
        _appearance.SetData(ent, SupermatterVisuals.Delaminating, true);

        var beforeDelaminateEv = new SupermatterBeforeDelaminatedEvent();
        RaiseLocalEvent(ent, beforeDelaminateEv);

        if (beforeDelaminateEv.Handled)
            return;

        var delaminatingEv = new SupermatterDelaminatedEvent();
        RaiseLocalEvent(ent, delaminatingEv);
    }

    public void ModifyEnergy(Entity<SupermatterEnergyComponent?> ent, float energy)
    {
        if (energy == 0f)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldEnergy = ent.Comp.CurrentEnergy;
        ent.Comp.CurrentEnergy += energy;

        var modifiedEv = new SupermatterEnergyModifiedEvent(ent.Comp.CurrentEnergy, oldEnergy);
        RaiseLocalEvent(ent, modifiedEv);
    }

    public void ModifyHeatResistance(Entity<SupermatterHeatResistanceComponent?> ent, float resistance)
    {
        if (resistance == 0f)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldResistance = ent.Comp.HeatResistance;
        ent.Comp.HeatResistance += resistance;

        var ev = new SupermatterHeatResistanceModifiedEvent(ent.Comp.HeatResistance, oldResistance);
        RaiseLocalEvent(ent, ev);
    }

    public void ModifyPowerTransmission(Entity<SupermatterPowerTransmissionComponent?> ent, float power)
    {
        if (power == 0f)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldPower = ent.Comp.CurrentPower;
        ent.Comp.CurrentPower += power;

        var ev = new SupermatterPowerTransmissionModifiedEvent(ent.Comp.CurrentPower, oldPower);
        RaiseLocalEvent(ent, ev);
    }

    public void ModifyHeatEnergy(Entity<SupermatterHeatEnergyComponent?> ent, float energy)
    {
        if (energy == 0f)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldEnergy = ent.Comp.CurrentEnergy;
        ent.Comp.CurrentEnergy += energy;

        var ev = new SupermatterHeatEnergyModifiedEvent(ent.Comp.CurrentEnergy, oldEnergy);
        RaiseLocalEvent(ent, ev);
    }

    public void ActivateSupermatter(Entity<SupermatterActiveComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Active == enabled)
            return;

        if (enabled)
        {
            var ev = new SupermatterActivatedEvent();
            RaiseLocalEvent(ent, ev);
        }
        else
        {
            var ev = new SupermatterDeactivatedEvent();
            RaiseLocalEvent(ent, ev);
        }

        ent.Comp.Active = enabled;
    }

    public bool IsActive(Entity<SupermatterActiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return true;

        return ent.Comp.Active;
    }

#endregion

}
