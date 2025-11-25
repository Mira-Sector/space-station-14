using Content.Shared.Arcade.Racer.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Arcade.Racer;

public abstract partial class SharedRacerArcadeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeComponent, AfterActivatableUIOpenEvent>(OnAfterUiOpen);
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

    [PublicAPI]
    public void NewGame(Entity<RacerArcadeComponent?> ent, IEnumerable<EntityUid> actors)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var startingStage = PrototypeMan.Index(ent.Comp.StartingStage);
        var startingNode = startingStage.Graph.Nodes[startingStage.Graph.StartingNode!];

        List<EntityUid> objects = new(actors.Count());
        foreach (var actor in actors)
        {
            var player = Spawn(ent.Comp.Player);
            Comp<RacerArcadeObjectComponent>(player).Position = startingNode.Position;
            EnsureComp<RacerArcadePlayerControlledComponent>(player).Controller = actor;
            objects.Add(player);
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
    public IEnumerable<Entity<T>> GetObjects<T>(Entity<RacerArcadeComponent?> arcade) where T : Component
    {
        if (!Resolve(arcade.Owner, ref arcade.Comp) || arcade.Comp.State is not { } state)
            yield break;

        foreach (var netObj in state.Objects)
        {
            if (!TryGetEntity(netObj, out var obj))
                continue;

            if (TryComp<T>(obj, out var comp))
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
}
