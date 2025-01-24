using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Surgery.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryRecieverComponent, LimbInitEvent>(OnLimbInit);
        SubscribeLocalEvent<SurgeryRecieverComponent, InteractUsingEvent>(OnLimbInteract);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, InteractUsingEvent>(OnBodyInteract);

        SubscribeLocalEvent<SurgeryRecieverComponent, SurgeryDoAfterEvent>(OnDoAfter);
    }

    private void OnLimbInit(EntityUid uid, SurgeryRecieverComponent component, LimbInitEvent args)
    {
        component.Graph = MergeGraphs(component.AvailableSurgeries);
        component.Graph.TryGetStaringNode(out component.CurrentNode);

        if (args.Part.Body is not {} body)
            return;

        EnsureComp<SurgeryRecieverBodyComponent>(body, out var surgeryBodyComp);
        surgeryBodyComp.Limbs.Add(uid);
    }

    private void OnLimbInteract(EntityUid uid, SurgeryRecieverComponent component, InteractUsingEvent args)
    {
        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        TryTraverseGraph(uid, component, body, args.User, args.Used);
    }

    private void OnBodyInteract(EntityUid uid, SurgeryRecieverBodyComponent component, InteractUsingEvent args)
    {
        if (!TryComp<DamagePartSelectorComponent>(args.User, out var damageSelectorComp))
            return;

        foreach (var limb in component.Limbs)
        {
            if (!TryComp<SurgeryRecieverComponent>(limb, out var surgeryComp))
                continue;

            if (!TryComp<BodyPartComponent>(limb, out var partComp) || partComp.Body != uid)
                continue;

            if (partComp.PartType != damageSelectorComp.SelectedPart.Type)
                continue;

            if (partComp.Symmetry != damageSelectorComp.SelectedPart.Side)
                continue;

            // may have multiple limbs so dont exit early
            TryTraverseGraph(limb, surgeryComp, uid, args.User, args.Used);
        }
    }

    private void OnDoAfter(EntityUid uid, SurgeryRecieverComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        args.Handled = true;

        DoNodeLeftSpecials(component.CurrentNode?.Special, body, uid);
        component.CurrentNode = args.TargetEdge.Connection;
        DoNodeReachedSpecials(component.CurrentNode?.Special, body, uid);

        component.DoAfters.Remove(args.DoAfter.Id);
    }

    public bool TryTraverseGraph(EntityUid uid, SurgeryRecieverComponent surgery, EntityUid body, EntityUid user, EntityUid used)
    {
        if (surgery.CurrentNode == null && !surgery.Graph.TryGetStaringNode(out surgery.CurrentNode))
            return false;

        foreach (var edge in surgery.CurrentNode.Edges)
        {
            // when merging the graph we made sure there arent multiple edges to traverse
            if (TryEdge(uid, surgery, edge, body, user, used))
                return true;
        }

        return false;
    }

    public bool TryEdge(EntityUid uid, SurgeryRecieverComponent surgery, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid used)
    {
        var requirementsPassed = RequirementsPassed(uid, edge, body, user, used);

        if (requirementsPassed == SurgeryEdgeState.Failed)
            return false;

        foreach (var doAfter in surgery.DoAfters)
        {
            _doAfter.Cancel(doAfter);
        }

        surgery.DoAfters.Clear();

        if (requirementsPassed == SurgeryEdgeState.DoAfter)
            return TryStartDoAfters(uid, surgery, edge, body, user, used);

        DoNodeLeftSpecials(surgery.CurrentNode?.Special, body, uid);
        surgery.CurrentNode = edge.Connection;
        DoNodeReachedSpecials(surgery.CurrentNode?.Special, body, uid);

        return true;
    }

    private void DoNodeReachedSpecials(SurgerySpecial[]? specials, EntityUid body, EntityUid limb)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
        {
            special.NodeReached(body, limb);
        }
    }

    private void DoNodeLeftSpecials(SurgerySpecial[]? specials, EntityUid body, EntityUid limb)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
        {
            special.NodeLeft(body, limb);
        }
    }

    public SurgeryEdgeState RequirementsPassed(EntityUid uid, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid? used)
    {
        var doAfters = 0;
        var directPassed = false;

        foreach (var requirement in edge.Requirements)
        {
            switch (requirement.RequirementMet(body, uid, user, used))
            {
                case SurgeryEdgeState.Failed:
                {
                    return SurgeryEdgeState.Failed;
                }
                case SurgeryEdgeState.DoAfter:
                {
                    doAfters++;
                    break;
                }
                case SurgeryEdgeState.Passed:
                {
                    directPassed = true;
                    break;
                }
            }
        }

        // we need to know if there is a fallback incase all doafters fail
        if (directPassed)
            return SurgeryEdgeState.Passed;

        if (doAfters >= 0)
            return SurgeryEdgeState.DoAfter;

        return SurgeryEdgeState.Failed;
    }

    private bool TryStartDoAfters(EntityUid uid, SurgeryRecieverComponent surgery, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid used)
    {
        var failedDoAfters = 0;

        foreach (var requirement in edge.Requirements)
        {
            if (!requirement.StartDoAfter(_doAfter, edge, body, uid, user, used, out var doAfter))
            {
                failedDoAfters++;
                continue;
            }

            surgery.DoAfters.Add(doAfter.Value);
        }

        if (failedDoAfters >= edge.Requirements.Count())
            return false;

        return true;
    }
}
