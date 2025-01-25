using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Surgery.Components;
using Robust.Shared.Prototypes;

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

        Dirty(uid, component);

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

        if (!component.Graph.TryFindNode(args.TargetEdge.Connection, out var newNode))
            return;

        args.Handled = true;

        DoNodeLeftSpecials(component.CurrentNode?.Special, body, uid);
        component.CurrentNode = newNode;
        DoNodeReachedSpecials(component.CurrentNode?.Special, body, uid);

        component.DoAfters.Remove(args.DoAfter.Id);

        Dirty(uid, component);
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
        var requirementsPassed = edge.Requirement.RequirementMet(body, uid, user, used);

        if (requirementsPassed == SurgeryEdgeState.Failed)
            return false;

        foreach (var doAfter in surgery.DoAfters)
        {
            _doAfter.Cancel(doAfter);
        }

        surgery.DoAfters.Clear();

        if (requirementsPassed == SurgeryEdgeState.DoAfter)
        {
            var doAfterStarted = edge.Requirement.StartDoAfter(_doAfter, edge, body, uid, user, used, out var doAfterId);

            if (doAfterId != null)
                surgery.DoAfters.Add(doAfterId.Value);

            Dirty(uid, surgery);
            return doAfterStarted;
        }

        if (!surgery.Graph.TryFindNode(edge.Connection, out var newNode))
            return false;

        DoNodeLeftSpecials(surgery.CurrentNode?.Special, body, uid);
        surgery.CurrentNode = newNode;
        DoNodeReachedSpecials(surgery.CurrentNode?.Special, body, uid);

        Dirty(uid, surgery);

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
}
