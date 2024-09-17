using Content.Server.Fluids.EntitySystems;
using Content.Server.Footprints.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.Footprint.Systems;


public sealed partial class FootprintSystem : EntitySystem
{

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    const string ShoeSlot = "shoes";

    public override void Update(float frametime)
    {
        var query = EntityQueryEnumerator<CanLeaveFootprintsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var currentFootprintComp, out var transform))
        {
            if (!CanLeaveFootprints(uid, out var messMaker) ||
                !TryComp<LeavesFootprintsComponent>(messMaker, out var footprintComp) ||
                currentFootprintComp.Solution.Comp.Solution.Volume <= 0)
                continue;

            EntityUid posUid;

            if (HasComp<ClothingComponent>(messMaker) &&
                _container.TryGetContainingContainer((messMaker, null, null), out var container) &&
                TryComp<TransformComponent>(container.Owner, out var xform))
            {
                posUid = container.Owner;
            }
            else
            {
                posUid = messMaker;
            }

            var angle = _transform.GetWorldRotation(posUid);
            var newPos = _transform.GetMapCoordinates(posUid);
            var oldPos = currentFootprintComp.LastFootstep;

            if (newPos == MapCoordinates.Nullspace)
                continue;

            if (newPos.MapId != oldPos.MapId)
            {
                DoFootprint(messMaker, currentFootprintComp, footprintComp, newPos, angle);
                return;
            }

            var delta = Vector2.Distance(newPos.Position, oldPos.Position);

            if (delta < footprintComp.Distance)
                continue;

            DoFootprint(messMaker, currentFootprintComp, footprintComp, newPos, angle);
        }
    }

    private void DoFootprint(EntityUid uid, CanLeaveFootprintsComponent currentFootprintComp, LeavesFootprintsComponent footprintComp, MapCoordinates pos, Angle angle)
    {
        var footprint = footprintComp.FootprintPrototype;

        if (currentFootprintComp.UseAlternative != null)
        {
            if (currentFootprintComp.UseAlternative.Value)
                footprint = footprintComp.FootprintPrototypeAlternative;

            currentFootprintComp.UseAlternative ^= true;
        }

        var footprintEnt = EntityManager.Spawn(footprint, pos, rotation: angle);

        if (currentFootprintComp.Container != null)
        {
            var footprintSolution = _solutionContainer.SplitSolution(currentFootprintComp.Solution, 1);
            _puddle.TryAddSolution(footprintEnt, footprintSolution, false, false);
        }

        _appearance.TryGetData<Color>(footprintEnt, PuddleVisuals.SolutionColor, out var color);
        color = color.WithAlpha(currentFootprintComp.Alpha);
        _appearance.SetData(footprintEnt, PuddleVisuals.SolutionColor, color);

        currentFootprintComp.FootstepsLeft -= 1;

        if (currentFootprintComp.FootstepsLeft <= 0)
        {
            RemComp<CanLeaveFootprintsComponent>(uid);
            return;
        }

        currentFootprintComp.LastFootstep = pos;
        currentFootprintComp.Alpha = (float) currentFootprintComp.FootstepsLeft / footprintComp.MaxFootsteps;
    }

    private bool CanLeaveFootprints(EntityUid uid, out EntityUid messMaker)
    {
        messMaker = EntityUid.Invalid;

        if (_inventory.TryGetSlotEntity(uid, ShoeSlot, out var shoe) &&
            EntityManager.HasComponent<LeavesFootprintsComponent>(shoe)) // check if their shoes have it too
        {
            messMaker = shoe.Value;
        }
        else if (EntityManager.HasComponent<LeavesFootprintsComponent>(uid))
        {
            if (shoe != null)
                RemComp<CanLeaveFootprintsComponent>(shoe.Value);

            messMaker = uid;
        }
        else
        {
            CleanupFootprintComp(uid, shoe);
            return false;
        }

        if (messMaker == EntityUid.Invalid ||
            !HasComp<LeavesFootprintsComponent>(messMaker))
        {
            CleanupFootprintComp(uid, shoe);
            return false;
        }

        return true;
    }

    private void CleanupFootprintComp(EntityUid player, EntityUid? shoe)
    {
        RemComp<CanLeaveFootprintsComponent>(player);

        if (shoe != null)
            RemComp<CanLeaveFootprintsComponent>(shoe.Value);
    }
}
