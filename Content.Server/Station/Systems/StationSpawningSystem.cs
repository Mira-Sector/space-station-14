using Content.Server.Access.Systems;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind.Commands;
using Content.Server.PDA;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.DetailExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Station.Systems;

/// <summary>
/// Manages spawning into the game, tracking available spawn points.
/// Also provides helpers for spawning in the player's mob.
/// </summary>
[PublicAPI]
public sealed class StationSpawningSystem : SharedStationSpawningSystem
{
    [Dependency] private readonly SharedAccessSystem _accessSystem = default!;
    [Dependency] private readonly ActorSystem _actors = default!;
    [Dependency] private readonly ArrivalsSystem _arrivalsSystem = default!;
    [Dependency] private readonly IdCardSystem _cardSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ContainerSpawnPointSystem _containerSpawnPointSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly PdaSystem _pdaSystem = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _randomizeCharacters;

    private Dictionary<SpawnPriorityPreference, Action<PlayerSpawningEvent>> _spawnerCallbacks = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_configurationManager, CCVars.ICRandomCharacters, e => _randomizeCharacters = e, true);

        _spawnerCallbacks = new Dictionary<SpawnPriorityPreference, Action<PlayerSpawningEvent>>()
        {
            { SpawnPriorityPreference.Arrivals, _arrivalsSystem.HandlePlayerSpawning },
            {
                SpawnPriorityPreference.Cryosleep, ev =>
                {
                    var stationTime = _timing.CurTime.Subtract(_gameTicker.RoundStartTimeSpan).Minutes;
                    var cutoff = _arrivalsSystem.ArrivalsCutoff;

                    if (cutoff >= stationTime)
                    {
                        _arrivalsSystem.HandlePlayerSpawning(ev);
                    }
                    else
                    {
                        _containerSpawnPointSystem.HandlePlayerSpawning(ev);
                    }
                }
            }
        };
    }

    /// <summary>
    /// Attempts to spawn a player character onto the given station.
    /// </summary>
    /// <param name="station">Station to spawn onto.</param>
    /// <param name="job">The job to assign, if any.</param>
    /// <param name="profile">The character profile to use, if any.</param>
    /// <param name="stationSpawning">Resolve pattern, the station spawning component for the station.</param>
    /// <returns>The resulting player character, if any.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    /// <remarks>
    /// This only spawns the character, and does none of the mind-related setup you'd need for it to be playable.
    /// </remarks>
    public EntityUid? SpawnPlayerCharacterOnStation(EntityUid? station, ProtoId<JobPrototype>? job, HumanoidCharacterProfile? profile, StationSpawningComponent? stationSpawning = null)
    {
        if (station != null && !Resolve(station.Value, ref stationSpawning))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var ev = new PlayerSpawningEvent(job, profile, station);

        if (station != null && profile != null)
        {
            // Try to call the character's preferred spawner first.
            if (_spawnerCallbacks.TryGetValue(profile.SpawnPriority, out var preferredSpawner))
            {
                preferredSpawner(ev);

                foreach (var (key, remainingSpawner) in _spawnerCallbacks)
                {
                    if (key == profile.SpawnPriority)
                        continue;

                    remainingSpawner(ev);
                }
            }
            else
            {
                // Call all of them in the typical order.
                foreach (var typicalSpawner in _spawnerCallbacks.Values)
                {
                    typicalSpawner(ev);
                }
            }
        }

        RaiseLocalEvent(ev);
        DebugTools.Assert(ev.SpawnResult is { Valid: true } or null);

        return ev.SpawnResult;
    }

    //TODO: Figure out if everything in the player spawning region belongs somewhere else.
    #region Player spawning helpers

    /// <summary>
    /// Spawns in a player's mob according to their job and character information at the given coordinates.
    /// Used by systems that need to handle spawning players.
    /// </summary>
    /// <param name="coordinates">Coordinates to spawn the character at.</param>
    /// <param name="job">Job to assign to the character, if any.</param>
    /// <param name="profile">Appearance profile to use for the character.</param>
    /// <param name="station">The station this player is being spawned on.</param>
    /// <param name="entity">The entity to use, if one already exists.</param>
    /// <returns>The spawned entity</returns>
    public EntityUid SpawnPlayerMob(
        EntityCoordinates coordinates,
        ProtoId<JobPrototype>? job,
        HumanoidCharacterProfile? profile,
        EntityUid? station,
        EntityUid? entity = null)
    {
        _prototypeManager.TryIndex(job ?? string.Empty, out var prototype);
        RoleLoadout? loadout = null;

        // Need to get the loadout up-front to handle names if we use an entity spawn override.
        var jobLoadout = LoadoutSystem.GetJobPrototype(prototype?.ID);

        if (_prototypeManager.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
        {
            profile?.Loadouts.TryGetValue(jobLoadout, out loadout);

            // Set to default if not present
            if (loadout == null)
            {
                loadout = new RoleLoadout(jobLoadout);
                loadout.SetDefault(profile, _actors.GetSession(entity), _prototypeManager);
            }
        }

        // If we're not spawning a humanoid, we're gonna exit early without doing all the humanoid stuff.
        if (prototype?.JobEntity != null)
        {
            DebugTools.Assert(entity is null);
            var jobEntity = SpawnEntity(prototype.JobEntity, coordinates, prototype);

            // Make sure custom names get handled, what is gameticker control flow whoopy.
            if (loadout != null)
            {
                EquipRoleName(jobEntity, loadout, roleProto!);

                if (roleProto != null)
                    EquipLoadout(jobEntity, jobLoadout, loadout, roleProto, prototype, profile);
            }

            return jobEntity;
        }

        profile?.Loadouts.TryGetValue(jobLoadout, out loadout);

        if (loadout != null && roleProto != null)
        {
            foreach (var group in loadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
            {
                foreach (var items in group.Value)
                {
                    if (!_prototypeManager.TryIndex(items.Prototype, out var loadoutProto))
                    {
                        continue;
                    }

                    if (loadoutProto.Entity != null)
                    {
                        var newEntity = SpawnEntity(loadoutProto.Entity, coordinates, prototype);
                        EquipLoadout(newEntity, jobLoadout, loadout, roleProto, prototype, profile);
                        EntityManager.DeleteEntity(entity);
                        return newEntity;
                    }
                }
            }
        }

        string speciesId = profile != null ? profile.Species : SharedHumanoidAppearanceSystem.DefaultSpecies;

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(speciesId, out var species))
            throw new ArgumentException($"Invalid species prototype was used: {speciesId}");

        entity ??= SpawnEntity(species.Prototype, coordinates, prototype);

        if (profile != null)
        {
            _humanoidSystem.LoadProfile(entity.Value, profile);
            _metaSystem.SetEntityName(entity.Value, profile.Name);

            if (profile.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            {
                AddComp<DetailExaminableComponent>(entity.Value).Content = profile.FlavorText;
            }
        }

        if (roleProto != null)
        {
            // Set to default if not present
            if (loadout == null)
            {
                loadout = new RoleLoadout(jobLoadout);
                loadout.SetDefault(profile, _actors.GetSession(entity), _prototypeManager);
            }

            EquipLoadout(entity.Value, jobLoadout, loadout, roleProto, prototype, profile);
        }

        var gearEquippedEv = new StartingGearEquippedEvent(entity.Value);
        RaiseLocalEvent(entity.Value, ref gearEquippedEv);

        if (prototype != null && TryComp(entity.Value, out MetaDataComponent? metaData))
            SetPdaAndIdCardData(entity.Value, metaData.EntityName, prototype, station);

        _identity.QueueIdentityUpdate(entity.Value);
        return entity.Value;
    }

    private EntityUid SpawnEntity(string prototype, EntityCoordinates coordinates, JobPrototype? job)
    {
        var entity = EntityManager.SpawnEntity(prototype, coordinates);
        MakeSentientCommand.MakeSentient(entity, EntityManager);

        if (job != null)
            DoJobSpecials(job, entity);

        _identity.QueueIdentityUpdate(entity);
        return entity;
    }

    private void EquipLoadout(EntityUid entity, string jobLoadout, RoleLoadout loadout, RoleLoadoutPrototype roleProto, JobPrototype? prototype, HumanoidCharacterProfile? profile)
    {
        EquipRoleLoadout(entity, loadout, roleProto);

        if (prototype?.StartingGear != null)
        {
            var startingGear = _prototypeManager.Index<StartingGearPrototype>(prototype.StartingGear);
            EquipStartingGear(entity, startingGear, raiseEvent: false);
        }

        var gearEquippedEv = new StartingGearEquippedEvent(entity);
        RaiseLocalEvent(entity, ref gearEquippedEv);

    }

    private void DoJobSpecials(JobPrototype job, EntityUid entity)
    {
        foreach (var jobSpecial in job.Special)
        {
            jobSpecial.AfterEquip(entity);
        }
    }

    /// <summary>
    /// Sets the ID card and PDA name, job, and access data.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="characterName">Character name to use for the ID.</param>
    /// <param name="job">Job to use for the PDA and ID.</param>
    /// <param name="station">The station this player is being spawned on.</param>
    public void SetPdaAndIdCardData(EntityUid entity, string characterName, JobPrototype? job, EntityUid? station)
    {
        if (job == null)
            return;

        if (!InventorySystem.TryGetSlotEntity(entity, "id", out var idUid))
            return;

        var cardId = idUid.Value;
        if (TryComp<PdaComponent>(idUid, out var pdaComponent) && pdaComponent.ContainedId != null)
        {
            cardId = pdaComponent.ContainedId.Value;
            Dirty(idUid.Value, pdaComponent);
        }

        if (!TryComp<IdCardComponent>(cardId, out var card))
            return;

        if (card.UpdateName)
        {
            _cardSystem.TryChangeFullName(cardId, characterName, card);
            _cardSystem.TryChangeJobTitle(cardId, job.LocalizedName, card);
        }

        if (_prototypeManager.TryIndex(job.Icon, out var jobIcon))
            _cardSystem.TryChangeJobIcon(cardId, jobIcon, card);

        if (card.AccessOverride)
        {
            var extendedAccess = false;
            if (station != null)
            {
                var data = Comp<StationJobsComponent>(station.Value);
                extendedAccess = data.ExtendedAccess;
            }

            _accessSystem.SetAccessToJob(cardId, job, extendedAccess);
        }

        if (pdaComponent != null)
            _pdaSystem.SetOwner(idUid.Value, pdaComponent, entity, characterName);
    }


    #endregion Player spawning helpers
}

/// <summary>
/// Ordered broadcast event fired on any spawner eligible to attempt to spawn a player.
/// This event's success is measured by if SpawnResult is not null.
/// You should not make this event's success rely on random chance.
/// This event is designed to use ordered handling. You probably want SpawnPointSystem to be the last handler.
/// </summary>
[PublicAPI]
public sealed class PlayerSpawningEvent : EntityEventArgs
{
    /// <summary>
    /// The entity spawned, if any. You should set this if you succeed at spawning the character, and leave it alone if it's not null.
    /// </summary>
    public EntityUid? SpawnResult;
    /// <summary>
    /// The job to use, if any.
    /// </summary>
    public readonly ProtoId<JobPrototype>? Job;
    /// <summary>
    /// The profile to use, if any.
    /// </summary>
    public readonly HumanoidCharacterProfile? HumanoidCharacterProfile;
    /// <summary>
    /// The target station, if any.
    /// </summary>
    public readonly EntityUid? Station;

    public PlayerSpawningEvent(ProtoId<JobPrototype>? job, HumanoidCharacterProfile? humanoidCharacterProfile, EntityUid? station)
    {
        Job = job;
        HumanoidCharacterProfile = humanoidCharacterProfile;
        Station = station;
    }
}
