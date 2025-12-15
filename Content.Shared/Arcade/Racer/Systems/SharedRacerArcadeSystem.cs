using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Arcade.Racer.Systems;

public abstract partial class SharedRacerArcadeSystem : EntitySystem
{
    [Dependency] private readonly RacerArcadeObjectCollisionSystem _collision = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<RacerArcadeObjectComponent> _data;
    private EntityQuery<RacerArcadeComponent> _arcade;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesBefore.Add(typeof(RacerArcadeObjectPhysicsSystem));

        SubscribeLocalEvent<RacerArcadeComponent, ComponentInit>(OnArcadeInit);
        SubscribeLocalEvent<RacerArcadeComponent, ComponentRemove>(OnArcadeRemove);

        SubscribeLocalEvent<RacerArcadeComponent, AfterActivatableUIOpenEvent>(OnArcadeAfterUiOpen);
        SubscribeLocalEvent<RacerArcadeComponent, BoundUIClosedEvent>(OnArcadeUiClose);

        SubscribeLocalEvent<RacerArcadeObjectComponent, ComponentInit>(OnObjectInit);

        _data = GetEntityQuery<RacerArcadeObjectComponent>();
        _arcade = GetEntityQuery<RacerArcadeComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RacerArcadeObjectComponent>();
        while (query.MoveNext(out var comp))
        {
            comp.PreviousPosition = comp.Position;
            comp.PreviousRotation = comp.Rotation;
        }
    }

    private void OnArcadeInit(Entity<RacerArcadeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Objects = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ObjectContainerId);
    }

    private void OnArcadeRemove(Entity<RacerArcadeComponent> ent, ref ComponentRemove args)
    {
        EndGameInternal(ent);
    }

    private void OnArcadeAfterUiOpen(Entity<RacerArcadeComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO: if this ever supports multiple players this logic is fundamentally flawed

        /*
         * there is a gamer among us
         * kinda sus
         *
         * they are relegated to the chair in the corner (spectating)
        */
        if (GetObjects<RacerArcadePlayerControlledComponent>(ent!).Any())
            return;

        NewGame(ent!, [args.Actor]);
    }

    private void OnArcadeUiClose(Entity<RacerArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(RacerGameUiKey.Key))
            return;

        if (!TryComp<RacerArcadeGamerComponent>(args.Actor, out var gamer) || gamer.Cabinet != ent.Owner)
            return;

        RemComp(args.Actor, gamer);
    }

    private void OnObjectInit(Entity<RacerArcadeObjectComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PreviousPosition = ent.Comp.Position;
        ent.Comp.PreviousRotation = ent.Comp.Rotation;
    }

    [PublicAPI]
    public void NewGame(Entity<RacerArcadeComponent?> ent, IEnumerable<EntityUid> players)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.State != null)
            EndGame(ent);

        var startingStage = PrototypeMan.Index(ent.Comp.StartingStage);
        var startingNode = startingStage.Graph.Nodes[startingStage.Graph.StartingNode!];

        List<EntityUid> objects = new(players.Count());
        ent.Comp.Players = new(players.Count());
        ent.Comp.State = new()
        {
            CurrentStage = ent.Comp.StartingStage,
            CurrentNode = startingNode,
            Objects = GetNetEntityList(objects)
        };

        foreach (var player in players)
        {
            var rot = startingStage.Graph.GetDirectionAtPosition(startingNode.Position);
            var euler = Quaternion.ToEulerRad(rot);
            var flatRot = Quaternion.FromAxisAngle(Vector3.UnitZ, euler.Z);

            var ship = SpawnObject(ent, ent.Comp.PlayerShipId, startingNode.Position, flatRot, false);

            // dont spawn inside the floor
            if (_collision.TryGetTrackHeightAtPosition(ship!, out var height))
                ship.Comp.Position.Z = height.Value;

            EnsureComp<RacerArcadePlayerControlledComponent>(ship, out var controlled);
            controlled.Controller = player;
            Dirty(ship, controlled);

            EnsureComp<RacerArcadeGamerComponent>(player, out var gamer);
            gamer.Cabinet = ent.Owner;
            Dirty(player, gamer);

            EntityManager.InitializeAndStartEntity(ship);

            ent.Comp.Players.Add(player);
        }

        Dirty(ent);
    }

    [PublicAPI]
    public void EndGame(Entity<RacerArcadeComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        EndGameInternal(ent!);
    }

    private void EndGameInternal(Entity<RacerArcadeComponent> ent)
    {
        foreach (var player in ent.Comp.Players)
            RemComp<RacerArcadeGamerComponent>(player);

        if (ent.Comp.State is not { } state)
            return;

        foreach (var obj in state.Objects)
            Del(GetEntity(obj));

        ent.Comp.State = null;
    }

    [PublicAPI]
    public Entity<RacerArcadeObjectComponent> SpawnObject(Entity<RacerArcadeComponent?> arcade, EntProtoId? objectId = null, Vector3? position = null, Quaternion? rotation = null, bool initialize = true)
    {
        if (!Resolve(arcade.Owner, ref arcade.Comp))
            return (EntityUid.Invalid, default!);

        if (arcade.Comp.State is not { } state)
            return (EntityUid.Invalid, default!);

        var data = new RacerArcadeObjectComponent();
        if (position != null)
            data.Position = position.Value;
        if (rotation != null)
            data.Rotation = rotation.Value;
        data.Arcade = arcade.Owner;

        /*
         * every object needs the component
         * howerer this is not normally possible
         * hence the manual initialization
        */
        var obj = EntityManager.CreateEntityUninitialized(objectId);
        AddComp(obj, data, true);
        FlagPredicted(obj);

        if (initialize)
            EntityManager.InitializeAndStartEntity(obj);

        state.Objects.Add(GetNetEntity(obj));
        _container.Insert(obj, arcade.Comp.Objects);
        Dirty(arcade);
        return (obj, data);
    }

    [PublicAPI]
    public void DeleteObject(Entity<RacerArcadeComponent?> arcade, EntityUid obj)
    {
        if (!Resolve(arcade.Owner, ref arcade.Comp))
            return;

        if (arcade.Comp.State is not { } state)
            return;

        if (!state.Objects.Remove(GetNetEntity(obj)))
            return;

        Dirty(arcade);
        _container.Remove(obj, arcade.Comp.Objects);
        Del(obj);
    }

    [PublicAPI]
    public IEnumerable<Entity<T>> GetObjects<T>(Entity<RacerArcadeComponent?> arcade) where T : Component
    {
        if (!Resolve(arcade.Owner, ref arcade.Comp) || arcade.Comp.State is not { } state)
            yield break;

        var query = GetEntityQuery<T>();
        foreach (var netObj in state.Objects)
        {
            if (!TryGetEntity(netObj, out var obj))
                continue;

            if (query.TryComp(obj.Value, out var comp))
                yield return (obj.Value, comp);
        }
    }

    [PublicAPI]
    public RacerArcadeObjectComponent GetData(EntityUid uid)
    {
        return _data.Comp(uid);
    }

    [PublicAPI]
    public bool TryGetControlledObject(Entity<RacerArcadeComponent?> arcade, EntityUid controller, [NotNullWhen(true)] out Entity<RacerArcadePlayerControlledComponent>? controlled)
    {
        foreach (var obj in GetObjects<RacerArcadePlayerControlledComponent>(arcade))
        {
            if (obj.Comp.Controller != controller)
                continue;

            controlled = obj;
            return true;
        }

        controlled = null;
        return false;
    }

    [PublicAPI]
    public bool TryGetArcade(Entity<RacerArcadeObjectComponent?> ent, [NotNullWhen(true)] out Entity<RacerArcadeComponent>? arcade)
    {
        if (!_data.Resolve(ent.Owner, ref ent.Comp))
        {
            arcade = null;
            return false;
        }

        var comp = _arcade.Get(ent.Comp.Arcade);
        arcade = (ent.Comp.Arcade, comp);
        return true;
    }
}
