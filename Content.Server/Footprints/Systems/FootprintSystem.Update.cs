using Content.Server.Footprints.Components;
using Content.Server.Forensics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Forensics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Footprints.Systems;

public sealed partial class FootprintSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private void InitializeUpdate()
    {
        SubscribeLocalEvent<FootprintComponent, ForensicScannerBeforeDoAfterEvent>(OnForensicScanner);

        SubscribeLocalEvent<GivesFootprintsComponent, StartCollideEvent>(OnStartStep);
        SubscribeLocalEvent<CanLeaveFootprintsComponent, ComponentInit>(FootprintRandom);
    }

    private void FootprintRandom(Entity<CanLeaveFootprintsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<LeavesFootprintsComponent>(ent.Owner, out var leavesFootprints))
        {
            RemComp<CanLeaveFootprintsComponent>(ent.Owner);
            return;
        }

        ent.Comp.FootprintIndex = _random.Next(leavesFootprints.FootprintPrototypes.Length);
    }

    private void OnStartStep(Entity<GivesFootprintsComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Container == null ||
        !CanLeaveFootprints(args.OtherEntity, out var messMaker, ent.Owner) ||
        !TryComp<LeavesFootprintsComponent>(messMaker, out var footprintComp) ||
        !TryComp<SolutionContainerManagerComponent>(ent.Owner, out var solutionManComp) ||
        solutionManComp.Containers.Count <=0)
            return;

        if (!GetSolution((ent.Owner, solutionManComp), ent.Comp.Container, out var puddleSolution) ||
        !TryComp<SolutionComponent>(puddleSolution, out var puddleSolutionComp))
            return;

        var playerFootprintComp = EnsureComp<CanLeaveFootprintsComponent>(messMaker);

        var footprintTotalUnits = footprintComp.MaxFootsteps * UnitsPerFootstep;

        if (!_solutionContainer.EnsureSolutionEntity(messMaker, ent.Comp.Container, out var newSolution, footprintTotalUnits) ||
        newSolution == null)
        {
            RemComp<CanLeaveFootprintsComponent>(messMaker);
            return;
        }

        playerFootprintComp.Solution = newSolution.Value;

        var split = _solutionContainer.SplitSolution(puddleSolution.Value, footprintTotalUnits);
        _solutionContainer.TryAddSolution(playerFootprintComp.Solution, split);
        playerFootprintComp.Solution.Comp.Solution.CanReact = false;

        playerFootprintComp.LastFootstep = _transform.GetMapCoordinates(args.OtherEntity);
        playerFootprintComp.FootstepsLeft = (uint)MathF.Floor((float)playerFootprintComp.Solution.Comp.Solution.Volume / UnitsPerFootstep);
        playerFootprintComp.Container = ent.Comp.Container;
        playerFootprintComp.LastPuddle = ent.Owner;
    }

    private void OnForensicScanner(Entity<FootprintComponent> ent, ref ForensicScannerBeforeDoAfterEvent args)
    {
        if (!TryComp<ResidueComponent>(ent.Owner, out var residue) || !TryComp<ForensicsComponent>(ent.Owner, out var forensics))
            return;

        //sorts the list by the age minimums
        //this we can assume first one that passes in the foreach is the biggest possible match
        var residueAge = residue.ResidueAge.OrderByDescending(x => x.AgeThrestholdMin);

        foreach (var i in residueAge)
        {
            var requiredTime = TimeSpan.FromMinutes(i.AgeThrestholdMin) + ent.Comp.CreationTime;

            if (requiredTime > _timing.CurTime)
                continue;

            forensics.Residues.Clear(); // cant get residues from any other way so just nuke it
            forensics.Residues.Add(Loc.GetString(i.AgeLocId));
            return;
        }
    }

    private bool GetSolution(Entity<SolutionContainerManagerComponent> ent, string container, out Entity<SolutionComponent>? targetSolutionComp)
    {
        foreach (var solutionComp in _solutionContainer.EnumerateSolutions(ent!))
        {
            if (solutionComp.Name != container)
                continue;

            targetSolutionComp = solutionComp.Solution;
            return true;
        }

        targetSolutionComp = null;
        return false;
    }
}
