using Content.Shared.Arcade.Racer.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Arcade.Racer.Systems;

public abstract partial class SharedRacerArcadeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUiOpen);
        SubscribeLocalEvent<RacerArcadeComponent, BoundUIClosedEvent>(OnUiClose);
    }

    private void OnAfterUiOpen(Entity<RacerArcadeComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
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

    private void OnUiClose(Entity<RacerArcadeComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(RacerGameUiKey.Key))
            return;

        if (!TryComp<RacerArcadeGamerComponent>(args.Actor, out var gamer) || gamer.Cabinet != ent.Owner)
            return;

        RemComp(args.Actor, gamer);
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
        foreach (var player in players)
        {
            var ship = SpawnObject(ent, ent.Comp.PlayerShipId, startingNode.Position);

            EnsureComp<RacerArcadePlayerControlledComponent>(ship, out var controlled);
            controlled.Controller = player;
            Dirty(ship, controlled);

            EnsureComp<RacerArcadeGamerComponent>(player, out var gamer);
            gamer.Cabinet = ent.Owner;
            Dirty(player, gamer);

            ent.Comp.Players.Add(player);
        }

        ent.Comp.State = new()
        {
            CurrentStage = ent.Comp.StartingStage,
            CurrentNode = startingNode,
            Objects = GetNetEntityList(objects)
        };

        Dirty(ent);
    }

    [PublicAPI]
    public void EndGame(Entity<RacerArcadeComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        foreach (var player in ent.Comp.Players)
            RemComp<RacerArcadeGamerComponent>(player);
    }

    [PublicAPI]
    public EntityUid SpawnObject(Entity<RacerArcadeComponent?> arcade, EntProtoId? objectId = null, Vector3? position = null, Quaternion? rotation = null)
    {
        if (!Resolve(arcade.Owner, ref arcade.Comp))
            return EntityUid.Invalid;

        var data = new RacerArcadeObjectComponent();
        if (position != null)
            data.Position = position.Value;
        if (rotation != null)
            data.Rotation = rotation.Value;
        data.Arcade = arcade.Owner;

        var obj = Spawn(objectId);
        AddComp(obj, data, true);

        arcade.Comp.State.Objects.Add(GetNetEntity(obj));
        Dirty(arcade);
        return obj;
    }

    [PublicAPI]
    public void DeleteObject(Entity<RacerArcadeComponent?> arcade, EntityUid obj)
    {
        if (!Resolve(arcade.Owner, ref arcade.Comp))
            return;

        if (!arcade.Comp.State.Objects.Remove(GetNetEntity(obj)))
            return;

        Dirty(arcade);
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
    public EntityUid GetArcade(Entity<RacerArcadeObjectComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return EntityUid.Invalid;

        return ent.Comp.Arcade;
    }
}
