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
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.Supermatter;

public sealed partial class SupermatterSystem : EntitySystem
{
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

        SubscribeLocalEvent<SupermatterDelaminationTeleportMapComponent, SupermatterDelaminationTeleportGetPositionEvent>(OnDelaminationTeleportGetPos);
        SubscribeLocalEvent<SupermatterDelaminationTeleportMapReturnComponent, ComponentInit>(OnMapReturnInit);

        SubscribeLocalEvent<SupermatterActiveComponent, MapInitEvent>(OnActiveInit);
        SubscribeLocalEvent<SupermatterActiveComponent, EntityConsumedByEventHorizonEvent>(OnActiveEventHorizon);

        SubscribeLocalEvent<SupermatterIntegerityComponent, ComponentInit>(OnIntegerityInit);

        SubscribeLocalEvent<SupermatterDelaminatableComponent, SupermatterDelaminatedEvent>(OnDelaminateableDelaminated);

        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterIntegerityModifiedEvent>(OnRadioIntegerityModified);
        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterCountdownTickEvent>(OnRadioCountdownTick);
        SubscribeLocalEvent<SupermatterDelaminationCountdownComponent, SupermatterBeforeDelaminatedEvent>(OnCountdownBeforeDelamination);
        SubscribeLocalEvent<SupermatterDelaminationCountdownComponent, SupermatterActivatedEvent>(OnCountdownActivated);
        SubscribeLocalEvent<SupermatterDelaminationCountdownComponent, SupermatterDeactivatedEvent>(OnCountdownDeactivated);

        SubscribeLocalEvent<SupermatterGasReactionComponent, AtmosExposedUpdateEvent>(OnGasReactionAtmosExposed);

        SubscribeLocalEvent<SupermatterGasEmitterComponent, ComponentInit>(OnGasEmitterInit);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterActivatedEvent>(OnGasEmitterActivated);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterDeactivatedEvent>(OnGasEmitterDeactivated);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterGasReactedEvent>(OnGasEmitterGasReact);
        SubscribeLocalEvent<SupermatterGasEmitterComponent, SupermatterSpaceGasReactedEvent>(OnGasEmitterSpaceReact);

        SubscribeLocalEvent<SupermatterEnergyCollideComponent, StartCollideEvent>(OnEnergyCollideCollide);
        SubscribeLocalEvent<SupermatterModifyEnergyOnCollideComponent, SupermatterEnergyCollidedEvent>(OnModifyEnergyCollide);

        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, ComponentInit>(OnArcShooterInit);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, SupermatterActivatedEvent>(OnArcShooterActivated);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, SupermatterDeactivatedEvent>(OnArcShooterDeactivated);
        SubscribeLocalEvent<SupermatterEnergyArcShooterComponent, AtmosExposedUpdateEvent>(OnArcShooterAtmosExposed);

        SubscribeLocalEvent<SupermatterRadiationComponent, ComponentInit>(OnRadiationInit);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterActivatedEvent>(OnRadiationActivated);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterDeactivatedEvent>(OnRadiationDeactivated);
        SubscribeLocalEvent<SupermatterRadiationComponent, SupermatterEnergyModifiedEvent>(OnRadiationEnergyModified);

        SubscribeLocalEvent<SupermatterEnergyDecayComponent, AtmosExposedUpdateEvent>(OnDecayAtmosExposed);
        SubscribeLocalEvent<SupermatterEnergyHeatGainComponent, AtmosExposedUpdateEvent>(OnHeatGainAtmosExposed);
        SubscribeLocalEvent<SupermatterModifyIntegerityOnEnergyComponent, SupermatterEnergyModifiedEvent>(OnEnergyIntegerityModifyEnergy);

        SubscribeLocalEvent<SupermatterSpawnOnIntegerityComponent, SupermatterActivatedEvent>(OnSpawnIntegerityActivated);
        SubscribeLocalEvent<SupermatterSpawnOnIntegerityComponent, SupermatterDeactivatedEvent>(OnSpawnIntegerityDeactivated);
        SubscribeLocalEvent<SupermatterSpawnOnIntegerityComponent, SupermatterIntegerityModifiedEvent>(OnSpawnIntegerityModified);

        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterActivatedEvent>(OnAudioActivated);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterDeactivatedEvent>(OnAudioDeactivated);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterBeforeDelaminatedEvent>(OnAudioBeforeDelamination);
        SubscribeLocalEvent<SupermatterAudioComponent, SupermatterIntegerityModifiedEvent>(OnAudioIntegerityModified);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var gasEmitterQuery = EntityQueryEnumerator<SupermatterGasEmitterComponent>();
        while (gasEmitterQuery.MoveNext(out var uid, out var gasEmitterComp))
        {
            if (!gasEmitterComp.Enabled)
                continue;

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
            if (!countdownComp.Active || !countdownComp.Enabled)
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

        var decayQuery = EntityQueryEnumerator<SupermatterEnergyDecayComponent, SupermatterEnergyComponent>();
        while (decayQuery.MoveNext(out var uid, out var decayComp, out var energyComp))
        {
            if (decayComp.NextDecay > _timing.CurTime)
                continue;

            decayComp.NextDecay += decayComp.Delay;

            ModifyEnergy((uid, energyComp), decayComp.EnergyDecay);
            decayComp.LastLostEnergy += decayComp.EnergyDecay;
        }

        var integeritySpawnQuery = EntityQueryEnumerator<SupermatterSpawnOnIntegerityComponent>();
        while (integeritySpawnQuery.MoveNext(out var uid, out var spawnComp))
        {
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

        var audioQuery = EntityQueryEnumerator<SupermatterAudioComponent>();
        while (audioQuery.MoveNext(out var uid, out var audioComp))
        {
            if (!audioComp.Enabled)
                return;

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
    }

    private void OnDelaminationTeleportGetPos(Entity<SupermatterDelaminationTeleportMapComponent> ent, ref SupermatterDelaminationTeleportGetPositionEvent args)
    {
        foreach (var (entity, pos) in args.Entities)
        {
            var mapUid = _map.CreateMap(out var mapId);
            _mapLoader.Load(mapId, ent.Comp.MapPath.ToString());
            _map.SetPaused(mapId, false);
            args.Entities[entity] = new MapCoordinates(ent.Comp.MapPosition, mapId);
        }

        args.Handled = true;
    }

    private void OnMapReturnInit(Entity<SupermatterDelaminationTeleportMapReturnComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextTeleport = _timing.CurTime + ent.Comp.Delay;
    }

    private void OnActiveInit(Entity<SupermatterActiveComponent> ent, ref MapInitEvent args)
    {
        ActivateSupermatter((ent, ent.Comp), ent.Comp.Active);
    }

    private static void OnIntegerityInit(Entity<SupermatterIntegerityComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Integerity = ent.Comp.MaxIntegrity;
    }

    private void OnGasEmitterInit(Entity<SupermatterGasEmitterComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.Delay;
        ent.Comp.CurrentRate = ent.Comp.MinRate;
        ent.Comp.CurrentTemperature = ent.Comp.MinTemperature;
    }

    private static void OnGasEmitterActivated(Entity<SupermatterGasEmitterComponent> ent, ref SupermatterActivatedEvent args)
    {
        ent.Comp.Enabled = true;
    }

    private static void OnGasEmitterDeactivated(Entity<SupermatterGasEmitterComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        ent.Comp.Enabled = false;
    }

    private void OnArcShooterInit(Entity<SupermatterEnergyArcShooterComponent> ent, ref ComponentInit args)
    {
        EnsureComp<LightningArcShooterComponent>(ent, out var arcShooterComp);
        ent.Comp.MinInterval = arcShooterComp.ShootMinInterval;
        ent.Comp.MaxInterval = arcShooterComp.ShootMaxInterval;
        arcShooterComp.Enabled = false;
    }

    private void OnArcShooterActivated(Entity<SupermatterEnergyArcShooterComponent> ent, ref SupermatterActivatedEvent args)
    {
        var arcShooterComp = EntityManager.GetComponent<LightningArcShooterComponent>(ent);
        arcShooterComp.Enabled = true;
        arcShooterComp.NextShootTime = _timing.CurTime;
        ent.Comp.Enabled = true;
    }

    private void OnArcShooterDeactivated(Entity<SupermatterEnergyArcShooterComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        EntityManager.GetComponent<LightningArcShooterComponent>(ent).Enabled = false;
        ent.Comp.Enabled = false;
    }

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

    private void OnGasReactionAtmosExposed(Entity<SupermatterGasReactionComponent> ent, ref AtmosExposedUpdateEvent args)
    {
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

        ent.Comp.LastReaction = _timing.CurTime;

        var ev = new SupermatterGasReactedEvent(completedReactions);
        RaiseLocalEvent(ent, ev);
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
        var match = GetRadioMessage<TimeSpan>(ent.Comp.CountdownMessages, args.Timer - args.ElapsedTime, true);

        if (match.Key == ent.Comp.LastCountdownMessage)
            return;

        ent.Comp.LastCountdownMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value, ("key", match.Key.TotalSeconds)), ent.Comp.Channel, ent);
    }

    private static KeyValuePair<T, LocId> GetRadioMessage<T>(Dictionary<T, LocId> messages, T comparison, bool positive) where T : IComparable<T>
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

    private static void OnCountdownActivated(Entity<SupermatterDelaminationCountdownComponent> ent, ref SupermatterActivatedEvent args)
    {
        ent.Comp.Enabled = true;
    }

    private static void OnCountdownDeactivated(Entity<SupermatterDelaminationCountdownComponent> ent, ref SupermatterDeactivatedEvent args)
    {
        ent.Comp.Enabled = false;
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
                    if (SupermatterGasEmitterComponent.ModifiableReactions.Contains(reaction.GetType()))
                        continue;
                }
            }

            ent.Comp.PreviousPercentage.Remove(gas);
        }
    }

    private static void OnGasEmitterSpaceReact(Entity<SupermatterGasEmitterComponent> ent, ref SupermatterSpaceGasReactedEvent args)
    {
        ent.Comp.PreviousPercentage.Clear();
    }

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

    private void OnArcShooterAtmosExposed(Entity<SupermatterEnergyArcShooterComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        var arcComp = EntityManager.GetComponent<LightningArcShooterComponent>(ent);
        arcComp.ShootMinInterval = ent.Comp.MinInterval;
        arcComp.ShootMaxInterval = ent.Comp.MaxInterval;
    }

    private void OnRadiationEnergyModified(Entity<SupermatterRadiationComponent> ent, ref SupermatterEnergyModifiedEvent args)
    {
        EntityManager.GetComponent<RadiationSourceComponent>(ent).Intensity = ent.Comp.Intensity + (float) Math.Pow((1 + ent.Comp.IntensityPower), args.CurrentEnergy);
    }

    private static void OnDecayAtmosExposed(Entity<SupermatterEnergyDecayComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        ent.Comp.LastLostEnergy = 0f;
    }

    private static void OnHeatGainAtmosExposed(Entity<SupermatterEnergyHeatGainComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        ent.Comp.CurrentGain = 0f;
    }

    private void OnActiveEventHorizon(Entity<SupermatterActiveComponent> ent, ref EntityConsumedByEventHorizonEvent args)
    {
        if (_whitelist.IsBlacklistPass(ent.Comp.ActivationBlackList, args.Entity))
            return;

        ActivateSupermatter((ent, ent.Comp), true);
    }

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
            spawn.CanSpawn = spawn.Min < integerity && spawn.Max >= integerity;

            if (spawn.CanSpawn)
                spawn.NextSpawn = _timing.CurTime + _random.Next(spawn.MinDelay, spawn.MaxDelay);
        }
    }

    private void OnAudioActivated(Entity<SupermatterAudioComponent> ent, ref SupermatterActivatedEvent args) => AudioAmbient(ent, true);

    private void OnAudioDeactivated(Entity<SupermatterAudioComponent> ent, ref SupermatterDeactivatedEvent args) => AudioAmbient(ent, false);

    private void AudioAmbient(Entity<SupermatterAudioComponent> ent, bool value)
    {
        _ambientSound.SetAmbience(ent, value);
        ent.Comp.Enabled = value;
        ent.Comp.NextPulseSound = _timing.CurTime;
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

    public void ModifyIntegerity(Entity<SupermatterIntegerityComponent?> ent, FixedPoint2 integerity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (integerity > 0 && ent.Comp.Integerity == ent.Comp.MaxIntegrity)
            return;

        var oldIntegerity = ent.Comp.Integerity;
        var newIntegerity = ent.Comp.Integerity + integerity;

        if (oldIntegerity == newIntegerity)
            return;

        if (newIntegerity > 0)
        {
            var modifiedEv = new SupermatterIntegerityModifiedEvent(newIntegerity, oldIntegerity, ent.Comp.MaxIntegrity);
            RaiseLocalEvent(ent, modifiedEv);

            ent.Comp.Integerity = newIntegerity > ent.Comp.MaxIntegrity ? ent.Comp.MaxIntegrity : newIntegerity;
            ent.Comp.IsDelaminating = false;
            return;
        }

        if (ent.Comp.IsDelaminating)
            return;

        ent.Comp.IsDelaminating = true;

        var beforeDelaminateEv = new SupermatterBeforeDelaminatedEvent();
        RaiseLocalEvent(ent, beforeDelaminateEv);

        if (beforeDelaminateEv.Handled)
            return;

        var delaminatingEv = new SupermatterDelaminatedEvent();
        RaiseLocalEvent(ent, delaminatingEv);
    }

    public void ModifyEnergy(Entity<SupermatterEnergyComponent?> ent, float energy)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldEnergy = ent.Comp.CurrentEnergy;
        var newEnergy = ent.Comp.CurrentEnergy + energy;

        if (oldEnergy == newEnergy)
            return;

        ent.Comp.CurrentEnergy = newEnergy;

        var modifiedEv = new SupermatterEnergyModifiedEvent(newEnergy, oldEnergy);
        RaiseLocalEvent(ent, modifiedEv);
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
}
