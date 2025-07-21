using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Standing;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem Ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        SubscribeLocalEvent<SurgeryReceiverComponent, ComponentInit>((u, c, a) => OnLimbInit(u, c));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, ComponentInit>((u, c, a) => OnBodyInit(u, c));

        SubscribeLocalEvent<SurgeryReceiverComponent, LimbInitEvent>((u, c, a) => OnLimbInit(u, c, a.Part.Comp));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, BodyInitEvent>((u, c, a) => OnBodyInit(u, c));

        SubscribeLocalEvent<SurgeryReceiverBodyComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<SurgeryReceiverComponent, InteractUsingEvent>((u, c, a) => OnLimbInteract(u, c, a.User, a.Used, a));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, InteractUsingEvent>((u, c, a) => OnBodyInteract(u, c, a.User, a.Used, a));

        SubscribeLocalEvent<SurgeryReceiverComponent, InteractHandEvent>((u, c, a) => OnLimbInteract(u, c, a.User, null, a));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, InteractHandEvent>((u, c, a) => OnBodyInteract(u, c, a.User, null, a));

        SubscribeLocalEvent<SurgeryReceiverComponent, SurgeryDoAfterEvent>(OnLimbDoAfter);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, SurgeryDoAfterEvent>(OnBodyDoAfter);
    }

    private void OnLimbInit(EntityUid uid, SurgeryReceiverComponent component, BodyPartComponent? bodyPartComp = null)
    {
        if (!Resolve(uid, ref bodyPartComp))
            return;

        component.Graph = MergeGraphs(component.AvailableSurgeries);
        component.Graph.TryGetStaringNode(out var startingNode);
        component.CurrentNode = startingNode;

        Dirty(uid, component);
    }

    private void OnBodyInit(EntityUid uid, SurgeryReceiverBodyComponent component)
    {
        foreach (var surgeries in component.Surgeries)
        {
            surgeries.Surgeries.Graph = MergeGraphs(surgeries.Surgeries.AvailableSurgeries);
            surgeries.Surgeries.Graph.TryGetStaringNode(out var startingNode);
            surgeries.Surgeries.CurrentNode = startingNode;
        }

        Dirty(uid, component);
    }

    private void OnBodyPartAdded(EntityUid uid, SurgeryReceiverBodyComponent component, ref BodyPartAddedEvent args)
    {
        if (!HasComp<SurgeryReceiverComponent>(args.Part))
            return;

        var netId = GetNetEntity(args.Part);

        foreach (var existingId in component.Limbs.Values)
        {
            if (netId == existingId)
                return;
        }

        component.Limbs.Add(new BodyPart(args.Part.Comp.PartType, args.Part.Comp.Symmetry), netId);
    }

    private void OnBodyPartRemoved(EntityUid uid, SurgeryReceiverBodyComponent component, ref BodyPartRemovedEvent args)
    {
        foreach (var limb in component.Limbs.Keys)
        {
            if (limb.Type != args.Part.Comp.PartType || limb.Side != args.Part.Comp.Symmetry)
                continue;

            component.Limbs.Remove(limb);
        }
    }

    private void OnLimbInteract(EntityUid uid, SurgeryReceiverComponent component, EntityUid user, EntityUid? used, HandledEntityEventArgs args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp))
            return;

        BodyPart bodyPart = new(bodyPartComp.PartType, bodyPartComp.Symmetry);

        args.Handled = TryTraverseGraph(uid, component, bodyPartComp.Body, user, used, bodyPart);
        Dirty(uid, component);
    }

    private void OnBodyInteract(EntityUid uid, SurgeryReceiverBodyComponent component, EntityUid user, EntityUid? used, HandledEntityEventArgs args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DamagePartSelectorComponent>(user, out var damageSelectorComp))
            return;

        if (!PreCheck(uid))
            return;

        var limbFound = false;

        foreach (var (bodyPart, netLimb) in component.Limbs)
        {
            var limb = GetEntity(netLimb);

            if (!TryComp<SurgeryReceiverComponent>(limb, out var surgeryComp))
                continue;

            if (!TryComp<BodyPartComponent>(limb, out var partComp) || partComp.Body != uid)
                continue;

            if (bodyPart.Type != damageSelectorComp.SelectedPart.Type)
                continue;

            if (bodyPart.Side != damageSelectorComp.SelectedPart.Side)
                continue;

            // we have a limb we can do surgery on
            limbFound = true;

            // may have multiple limbs so dont exit early
            args.Handled |= TryTraverseGraph(limb, surgeryComp, uid, user, used, bodyPart);
        }

        // if we have a possible limb they may have nothing do do
        // so we dont logically or with the TryTraverseGraph
        if (limbFound)
            return;

        // the body may have a surgery to persue instead
        foreach (var surgeries in component.Surgeries)
        {
            if (surgeries.BodyPart.Type != damageSelectorComp.SelectedPart.Type)
                continue;

            if (surgeries.BodyPart.Side != damageSelectorComp.SelectedPart.Side)
                continue;

            if (TryTraverseGraph(null, surgeries.Surgeries, uid, user, used, surgeries.BodyPart))
            {
                args.Handled = true;
                return;
            }
        }
    }

    private bool PreCheck(EntityUid uid)
    {
        if (TryComp<StandingStateComponent>(uid, out var standingComp) && standingComp.Standing)
            return false;

        return true;
    }

    private void OnBodyDoAfter(EntityUid uid, SurgeryReceiverBodyComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Handled)
            return;

        foreach (var surgeries in component.Surgeries)
        {
            // this is fucking abysmal but doafterid isnt serializable
            var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
            if (!surgeries.Surgeries.DoAfters.ContainsKey(netDoAfterId))
                continue;

            if (args.Cancelled)
            {
                surgeries.Surgeries.DoAfters.Remove(netDoAfterId);
                continue;
            }

            OnDoAfter(null, uid, surgeries.Surgeries, args);
        }
    }

    private void OnLimbDoAfter(EntityUid uid, SurgeryReceiverComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
            component.DoAfters.Remove(netDoAfterId);

            return;
        }

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp))
            return;

        if (args.BodyPart.Type != bodyPartComp.PartType || args.BodyPart.Side != bodyPartComp.Symmetry)
            return;

        OnDoAfter(uid, bodyPartComp.Body, component, args);
    }

    private void OnDoAfter(EntityUid? limb, EntityUid? body, ISurgeryReceiver surgeryReceiver, SurgeryDoAfterEvent args)
    {
        if (!surgeryReceiver.Graph.TryFindNode(args.TargetEdge.Connection, out var newNode))
            return;

        args.Handled = true;

        var oldNode = surgeryReceiver.CurrentNode;

        DoNodeLeftSpecials(surgeryReceiver.CurrentNode?.Special, body, limb, args.User, args.Used, args.BodyPart);
        surgeryReceiver.CurrentNode = newNode;
        DoNodeReachedSpecials(surgeryReceiver.CurrentNode?.Special, body, limb, args.User, args.Used, args.BodyPart);

        var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
        surgeryReceiver.DoAfters.Remove(netDoAfterId);
        CancelDoAfters(limb ?? body, surgeryReceiver);

        RaiseNodeModifiedEvents(limb, body, surgeryReceiver, oldNode!, newNode, args.TargetEdge);
    }

    public bool TryTraverseGraph(EntityUid? limb, ISurgeryReceiver surgery, EntityUid? body, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (surgery.CurrentNode == null)
        {
            if (!surgery.Graph.TryGetStaringNode(out var startingNode))
                return false;

            surgery.CurrentNode = startingNode;
        }

        Enum? ui = null;

        var oldNode = surgery.CurrentNode;

        foreach (var edge in surgery.CurrentNode.Edges)
        {
            // when merging the graph we made sure there arent multiple edges to traverse
            switch (TryEdge(limb, surgery, edge, body, user, used, bodyPart, out var edgeUi))
            {
                case SurgeryEdgeState.Passed:
                case SurgeryEdgeState.DoAfter:
                    RaiseNodeModifiedEvents(limb, body, surgery, oldNode!, surgery.CurrentNode, edge);
                    return true;
                case SurgeryEdgeState.UserInterface:
                    ui ??= edgeUi;
                    RaiseNodeModifiedEvents(limb, body, surgery, oldNode!, surgery.CurrentNode, edge);
                    break;
            }
        }

        if (ui != null)
        {
            var uiUid = limb ?? body;

            if (uiUid == null)
                return false;

            Ui.TryOpenUi(uiUid.Value, ui, user);
            return true;
        }

        return false;
    }

    public SurgeryEdgeState TryEdge(EntityUid? limb, ISurgeryReceiver surgery, SurgeryEdge edge, EntityUid? body, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        foreach (var (doAfterId, (doAfterNetUser, requirement)) in surgery.DoAfters)
        {
            if (requirement != edge.Requirement)
                continue;

            var doAfterUser = GetEntity(doAfterNetUser);

            // yes its passed
            // its valid as we are already doing something
            if (doAfterUser == user)
                return SurgeryEdgeState.Passed;
        }

        var requirementsPassed = edge.Requirement.RequirementMet(body, limb, user, used, bodyPart, out ui);

        if (requirementsPassed == SurgeryEdgeState.Failed)
            return SurgeryEdgeState.Failed;

        if (requirementsPassed == SurgeryEdgeState.UserInterface)
        {
            if (ui == null)
                return SurgeryEdgeState.Failed;

            surgery.UserInterfaces.Add(ui);

            return SurgeryEdgeState.UserInterface;
        }

        CancelDoAfters(limb ?? body, surgery);

        if (requirementsPassed == SurgeryEdgeState.DoAfter)
        {
            var doAfterStarted = edge.Requirement.StartDoAfter(_doAfter, edge, body, limb, user, used, bodyPart, out var doAfterId);

            if (doAfterId != null)
            {
                var netDoAfterId = (GetNetEntity(doAfterId.Value.Uid), doAfterId.Value.Index);
                surgery.DoAfters.Add(netDoAfterId, (GetNetEntity(user), edge.Requirement));
            }

            return doAfterStarted ? SurgeryEdgeState.DoAfter : SurgeryEdgeState.Failed;
        }

        if (!surgery.Graph.TryFindNode(edge.Connection, out var newNode))
            return SurgeryEdgeState.Failed;

        DoNodeLeftSpecials(surgery.CurrentNode?.Special, body, limb, user, used, bodyPart);
        surgery.CurrentNode = newNode;
        DoNodeReachedSpecials(surgery.CurrentNode?.Special, body, limb, user, used, bodyPart);

        return SurgeryEdgeState.Passed;
    }

    private static void DoNodeReachedSpecials(HashSet<SurgerySpecial>? specials, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
            special.NodeReached(body, limb, user, used, bodyPart);
    }

    private static void DoNodeLeftSpecials(HashSet<SurgerySpecial>? specials, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
            special.NodeLeft(body, limb, user, used, bodyPart);
    }

    private void CancelDoAfters(EntityUid? uid, ISurgeryReceiver surgeryReceiver)
    {
        foreach (var netDoAfter in surgeryReceiver.DoAfters.Keys)
        {
            var doAfter = new DoAfterId(GetEntity(netDoAfter.Item1), netDoAfter.Item2);
            if (!_doAfter.IsRunning(doAfter))
                continue;

            _doAfter.Cancel(doAfter);
        }

        if (uid == null)
            return;

        surgeryReceiver.DoAfters.Clear();

        foreach (var ui in surgeryReceiver.UserInterfaces)
            Ui.CloseUi(uid.Value, ui);

        surgeryReceiver.UserInterfaces.Clear();
    }

    private void RaiseNodeModifiedEvents(EntityUid? limb, EntityUid? body, ISurgeryReceiver receiver, SurgeryNode previousNode, SurgeryNode currentNode, SurgeryEdge edge)
    {
        if (limb != null)
        {
            var limbEv = new SurgeryCurrentNodeModifiedEvent(previousNode, currentNode, edge, receiver.Graph);
            RaiseLocalEvent(limb.Value, ref limbEv);
        }

        if (body != null)
        {
            var bodyEv = new SurgeryBodyCurrentNodeModifiedEvent(previousNode, currentNode, edge, receiver.Graph);
            RaiseLocalEvent(body.Value, ref bodyEv);
        }
    }
}
