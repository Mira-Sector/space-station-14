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
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, BodyInitEvent>(OnBodyInit);
        SubscribeLocalEvent<SurgeryRecieverComponent, InteractUsingEvent>(OnLimbInteract);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, InteractUsingEvent>(OnBodyInteract);

        SubscribeLocalEvent<SurgeryRecieverComponent, SurgeryDoAfterEvent>(OnLimbDoAfter);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, SurgeryDoAfterEvent>(OnBodyDoAfter);
    }

    private void OnLimbInit(EntityUid uid, SurgeryRecieverComponent component, LimbInitEvent args)
    {
        component.Graph = MergeGraphs(component.AvailableSurgeries);
        component.Graph.TryGetStaringNode(out var startingNode);
        component.CurrentNode = startingNode;

        Dirty(uid, component);

        if (args.Part.Body is not {} body)
            return;

        EnsureComp<SurgeryRecieverBodyComponent>(body, out var surgeryBodyComp);
        surgeryBodyComp.Limbs.Add(uid);
    }

    // as no surgeries can be added from the limbs we dont need to listen to ComponentAdded
    private void OnBodyInit(EntityUid uid, SurgeryRecieverBodyComponent component, BodyInitEvent args)
    {
        foreach (var surgeries in component.Surgeries)
        {
            surgeries.Surgeries.Graph = MergeGraphs(surgeries.Surgeries.AvailableSurgeries);
            surgeries.Surgeries.Graph.TryGetStaringNode(out var startingNode);
            surgeries.Surgeries.CurrentNode = startingNode;
        }

        Dirty(uid, component);
    }

    private void OnLimbInteract(EntityUid uid, SurgeryRecieverComponent component, InteractUsingEvent args)
    {
        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        BodyPart bodyPart = new(bodyPartComp.PartType, bodyPartComp.Symmetry);

        TryTraverseGraph(uid, component, body, args.User, args.Used, bodyPart);
        Dirty(uid, component);
    }

    private void OnBodyInteract(EntityUid uid, SurgeryRecieverBodyComponent component, InteractUsingEvent args)
    {
        if (!TryComp<DamagePartSelectorComponent>(args.User, out var damageSelectorComp))
            return;

        var limbHandled = false;

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

            BodyPart bodyPart = new(partComp.PartType, partComp.Symmetry);

            // may have multiple limbs so dont exit early
            limbHandled |= TryTraverseGraph(limb, surgeryComp, uid, args.User, args.Used, bodyPart);
        }

        if (limbHandled)
            return;

        // the body may have a surgery to persue instead
        foreach (var surgeries in component.Surgeries)
        {
            if (surgeries.BodyPart.Type != damageSelectorComp.SelectedPart.Type)
                continue;

            if (surgeries.BodyPart.Side != damageSelectorComp.SelectedPart.Side)
                continue;

            if (TryTraverseGraph(uid, surgeries.Surgeries, uid, args.User, args.Used, surgeries.BodyPart))
                return;
        }
    }

    private void OnBodyDoAfter(EntityUid uid, SurgeryRecieverBodyComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        foreach (var surgeries in component.Surgeries)
        {
            OnDoAfter(null, uid, surgeries.Surgeries, args);
        }
    }

    private void OnLimbDoAfter(EntityUid uid, SurgeryRecieverComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        if (args.BodyPart.Type != bodyPartComp.PartType || args.BodyPart.Side != bodyPartComp.Symmetry)
            return;

        OnDoAfter(uid, body, component, args);
    }

    private void OnDoAfter(EntityUid? limb, EntityUid body, ISurgeryReciever surgeryReciever, SurgeryDoAfterEvent args)
    {
        if (!surgeryReciever.Graph.TryFindNode(args.TargetEdge.Connection, out var newNode))
            return;

        args.Handled = true;

        DoNodeLeftSpecials(surgeryReciever.CurrentNode?.Special, body, limb, args.User, args.Used, args.BodyPart);
        surgeryReciever.CurrentNode = newNode;
        DoNodeReachedSpecials(surgeryReciever.CurrentNode?.Special, body, limb, args.User, args.Used, args.BodyPart);

        surgeryReciever.DoAfters.Remove(args.DoAfter.Id);
        CancelDoAfters(surgeryReciever);
    }

    public bool TryTraverseGraph(EntityUid? limb, ISurgeryReciever surgery, EntityUid body, EntityUid user, EntityUid used, BodyPart bodyPart)
    {
        if (surgery.CurrentNode == null)
        {
            if (!surgery.Graph.TryGetStaringNode(out var startingNode))
                return false;

            surgery.CurrentNode = startingNode;
        }

        foreach (var edge in surgery.CurrentNode.Edges)
        {
            // when merging the graph we made sure there arent multiple edges to traverse
            if (TryEdge(limb, surgery, edge, body, user, used, bodyPart))
                return true;
        }

        return false;
    }

    public bool TryEdge(EntityUid? limb, ISurgeryReciever surgery, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid used, BodyPart bodyPart)
    {
        var requirementsPassed = edge.Requirement.RequirementMet(body, limb, user, used, bodyPart);

        if (requirementsPassed == SurgeryEdgeState.Failed)
            return false;

        CancelDoAfters(surgery);

        if (requirementsPassed == SurgeryEdgeState.DoAfter)
        {
            var doAfterStarted = edge.Requirement.StartDoAfter(_doAfter, edge, body, limb, user, used, bodyPart, out var doAfterId);

            if (doAfterId != null)
                surgery.DoAfters.Add(doAfterId.Value);

            return doAfterStarted;
        }

        if (!surgery.Graph.TryFindNode(edge.Connection, out var newNode))
            return false;

        DoNodeLeftSpecials(surgery.CurrentNode?.Special, body, limb, user, used, bodyPart);
        surgery.CurrentNode = newNode;
        DoNodeReachedSpecials(surgery.CurrentNode?.Special, body, limb, user, used, bodyPart);

        return true;
    }

    private void DoNodeReachedSpecials(SurgerySpecial[]? specials, EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
        {
            special.NodeReached(body, limb, user, used, bodyPart);
        }
    }

    private void DoNodeLeftSpecials(SurgerySpecial[]? specials, EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
        {
            special.NodeLeft(body, limb, user, used, bodyPart);
        }
    }

    private void CancelDoAfters(ISurgeryReciever surgeryReciever)
    {
        foreach (var doAfter in surgeryReciever.DoAfters)
        {
            _doAfter.Cancel(doAfter);
        }

        surgeryReciever.DoAfters.Clear();
    }

}
