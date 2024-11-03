using Content.Server.Forensics;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Footprints.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Inventory;
using Content.Shared.Gravity;
using Content.Shared.Slippery;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;
using System.Runtime;

namespace Content.Server.Footprint.Systems;

public sealed partial class FootprintSystem : EntitySystem
{

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;


    const string ShoeSlot = "shoes";

    const float UnitsPerFootstep = 0.1f;

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

            if (_gravity.IsWeightless(posUid))
                continue;

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
            var footprintSolution = _solutionContainer.SplitSolution(currentFootprintComp.Solution, UnitsPerFootstep);
            _puddle.TryAddSolution(footprintEnt, footprintSolution, false, false);
        }

        _appearance.TryGetData<Color>(footprintEnt, PuddleVisuals.SolutionColor, out var color);
        color = color.WithAlpha(currentFootprintComp.Alpha);
        _appearance.SetData(footprintEnt, PuddleVisuals.SolutionColor, color);

        if (TryComp<FootprintComponent>(footprintEnt, out var footprintEntComp))
        {
            UpdateForensics(footprintEnt, uid);
            footprintEntComp.CreationTime = _timing.CurTime;
        }

        currentFootprintComp.FootstepsLeft -= 1;

        if (currentFootprintComp.FootstepsLeft <= 0 ||
            currentFootprintComp.FootstepsLeft > footprintComp.MaxFootsteps) // underflow :godo:
        {
            RemComp<CanLeaveFootprintsComponent>(uid);
            return;
        }

        currentFootprintComp.LastFootstep = pos;
        currentFootprintComp.Alpha = (float) currentFootprintComp.FootstepsLeft / footprintComp.MaxFootsteps;
    }

    private void UpdateForensics(EntityUid footprint, EntityUid messmaker)
    {
        if (!TryComp<ResidueComponent>(messmaker, out var messmakerResidueComp) ||
            messmakerResidueComp.ResidueAge == null)
            return;

        var footprintResidueComp = EnsureComp<ResidueComponent>(footprint);

        footprintResidueComp.ResidueAdjective = messmakerResidueComp.ResidueAdjective;
        footprintResidueComp.ResidueAge = messmakerResidueComp.ResidueAge;
    }

    public bool CanLeaveFootprints(EntityUid uid, out EntityUid messMaker, EntityUid? puddle = null)
    {
        messMaker = GetMessMaker(uid);

        if (messMaker == EntityUid.Invalid)
            return false;

        if (TryComp<CanLeaveFootprintsComponent>(messMaker, out var footprintComp) &&
            footprintComp.LastPuddle == puddle)
            return false;

        return true;
    }

    public EntityUid GetMessMaker(EntityUid uid)
    {
        if (_inventory.TryGetSlotEntity(uid, ShoeSlot, out var shoe) &&
            HasComp<LeavesFootprintsComponent>(shoe)) // check if their shoes have it too
        {
            if (HasComp<NoSlipComponent>(shoe))
            {
                RemComp<CanLeaveFootprintsComponent>(shoe.Value);
                return EntityUid.Invalid;
            }

            return shoe.Value;
        }
        else if (HasComp<LeavesFootprintsComponent>(uid))
        {
            if (shoe != null)
                RemComp<CanLeaveFootprintsComponent>(shoe.Value);

            return uid;
        }
        else
        {
            RemComp<CanLeaveFootprintsComponent>(uid);

            if (shoe != null)
                RemComp<CanLeaveFootprintsComponent>(shoe.Value);

            return EntityUid.Invalid;
        }
    }
}
