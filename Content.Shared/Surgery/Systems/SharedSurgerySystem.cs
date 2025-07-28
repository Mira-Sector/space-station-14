using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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

        SubscribeLocalEvent<SurgeryReceiverComponent, SurgeryEdgeRequirementDoAfterEvent>(OnLimbEdgeRequirementDoAfter);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, SurgeryEdgeRequirementDoAfterEvent>(OnBodyEdgeRequirementDoAfter);
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

        if (!PreCheck(component, uid, bodyPartComp.Body, used, user, bodyPart))
            return;

        if (DoInteractSpecials(component, bodyPartComp.Body, uid, user, used, bodyPart))
        {
            args.Handled = true;
            return;
        }

        args.Handled = TryTraverseGraph(uid, component, bodyPartComp.Body, user, used, bodyPart);
        Dirty(uid, component);
    }

    private void OnBodyInteract(EntityUid uid, SurgeryReceiverBodyComponent component, EntityUid user, EntityUid? used, HandledEntityEventArgs args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DamagePartSelectorComponent>(user, out var damageSelectorComp))
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

            if (!PreCheck(surgeryComp, null, uid, used, user, damageSelectorComp.SelectedPart))
                return;

            if (DoInteractSpecials(surgeryComp, uid, limb, user, used, bodyPart))
            {
                args.Handled = true;
                continue;
            }

            // may have multiple limbs so dont exit early
            args.Handled |= TryTraverseGraph(limb, surgeryComp, uid, user, used, bodyPart);
        }

        // if we have a possible limb they may have nothing do do
        // so we dont logically or with the TryTraverseGraph
        if (limbFound)
            return;

        // the body may have a surgery to pursue instead
        foreach (var surgeries in component.Surgeries)
        {
            if (surgeries.BodyPart.Type != damageSelectorComp.SelectedPart.Type)
                continue;

            if (surgeries.BodyPart.Side != damageSelectorComp.SelectedPart.Side)
                continue;

            if (!PreCheck(surgeries.Surgeries, null, uid, used, user, damageSelectorComp.SelectedPart))
                return;

            if (!DoInteractSpecials(surgeries.Surgeries, uid, null, user, used, surgeries.BodyPart))
            {
                if (!TryTraverseGraph(null, surgeries.Surgeries, uid, user, used, surgeries.BodyPart))
                    continue;
            }

            args.Handled = true;
            return;
        }
    }

    private bool PreCheck(ISurgeryReceiver receiver, EntityUid? limb, EntityUid? body, EntityUid? used, EntityUid user, BodyPart bodyPart)
    {
        var ev = new SurgeryInteractionAttemptEvent(body, limb, used, user, bodyPart);
        if (limb != null)
            RaiseLocalEvent(limb.Value, ref ev);
        if (body != null)
            RaiseLocalEvent(body.Value, ref ev);

        if (!ev.Cancelled || ev.Reason == null || receiver.CurrentNode is not { } currentNode)
            return !ev.Cancelled;

        // check if we could have even progressed
        // if not dont show the reason to stop irrelevant messages showing
        foreach (var edge in currentNode.Edges)
        {
            if (edge.Requirement.RequirementMet(body, limb, user, used, bodyPart, out _, true) == SurgeryInteractionState.Failed)
                continue;

            _popup.PopupPredicted(ev.Reason, (body ?? limb)!.Value, user);
            break;
        }

        return false;
    }

    private void OnBodyEdgeRequirementDoAfter(EntityUid uid, SurgeryReceiverBodyComponent component, SurgeryEdgeRequirementDoAfterEvent args)
    {
        if (args.Handled)
            return;

        foreach (var surgeries in component.Surgeries)
        {
            // this is fucking abysmal but doafterid isnt serializable
            var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
            if (!surgeries.Surgeries.EdgeDoAfters.ContainsKey(netDoAfterId))
                continue;

            if (args.Cancelled)
            {
                surgeries.Surgeries.EdgeDoAfters.Remove(netDoAfterId);
                continue;
            }

            OnEdgeRequirementDoAfter(null, uid, surgeries.Surgeries, args);
        }
    }

    private void OnLimbEdgeRequirementDoAfter(EntityUid uid, SurgeryReceiverComponent component, SurgeryEdgeRequirementDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
            component.EdgeDoAfters.Remove(netDoAfterId);

            return;
        }

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp))
            return;

        if (args.BodyPart.Type != bodyPartComp.PartType || args.BodyPart.Side != bodyPartComp.Symmetry)
            return;

        OnEdgeRequirementDoAfter(uid, bodyPartComp.Body, component, args);
    }

    private void OnEdgeRequirementDoAfter(EntityUid? limb, EntityUid? body, ISurgeryReceiver surgeryReceiver, SurgeryEdgeRequirementDoAfterEvent args)
    {
        if (!surgeryReceiver.Graph.TryFindNode(args.TargetEdge.Connection, out var newNode))
            return;

        args.Handled = true;

        var oldNode = surgeryReceiver.CurrentNode;

        DoNodeLeftSpecials(surgeryReceiver, body, limb, args.User, args.Used, args.BodyPart);
        surgeryReceiver.CurrentNode = newNode;
        DoNodeReachedSpecials(surgeryReceiver, body, limb, args.User, args.Used, args.BodyPart);

        var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
        surgeryReceiver.EdgeDoAfters.Remove(netDoAfterId);
        CancelEdgeRequirementDoAfters(surgeryReceiver);
        CancelNodeSpecialDoAfters(surgeryReceiver, oldNode);
        CloseAllUis(limb ?? body, surgeryReceiver);

        TryDealPain(args.TargetEdge.Requirement.Pain, body, limb, args.Used);

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
                case SurgeryInteractionState.Passed:
                    TryDealPain(edge.Requirement.Pain, body, limb, used);
                    RaiseNodeModifiedEvents(limb, body, surgery, oldNode!, surgery.CurrentNode, edge);
                    return true;
                case SurgeryInteractionState.DoAfter:
                    return true;
                case SurgeryInteractionState.UserInterface:
                    ui ??= edgeUi;
                    TryDealPain(edge.Requirement.Pain, body, limb, used);
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

    public SurgeryInteractionState TryEdge(EntityUid? limb, ISurgeryReceiver surgery, SurgeryEdge edge, EntityUid? body, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        foreach (var (_, (doAfterNetUser, requirement)) in surgery.EdgeDoAfters)
        {
            if (requirement != edge.Requirement)
                continue;

            var doAfterUser = GetEntity(doAfterNetUser);

            // yes its passed
            // its valid as we are already doing something
            if (doAfterUser == user)
                return SurgeryInteractionState.Passed;
        }

        var requirementsPassed = edge.Requirement.RequirementMet(body, limb, user, used, bodyPart, out ui);

        if (requirementsPassed == SurgeryInteractionState.Failed)
            return SurgeryInteractionState.Failed;

        if (requirementsPassed == SurgeryInteractionState.UserInterface)
        {
            if (ui == null)
                return SurgeryInteractionState.Failed;

            surgery.UserInterfaces.Add(ui);

            return SurgeryInteractionState.UserInterface;
        }

        CancelEdgeRequirementDoAfters(surgery);
        CloseAllUis(limb ?? body, surgery);

        if (requirementsPassed == SurgeryInteractionState.DoAfter)
        {
            var doAfterStarted = edge.Requirement.StartDoAfter(_doAfter, edge, body, limb, user, used, bodyPart, out var doAfterId);

            if (doAfterId != null)
            {
                var netDoAfterId = (GetNetEntity(doAfterId.Value.Uid), doAfterId.Value.Index);
                surgery.EdgeDoAfters.Add(netDoAfterId, (GetNetEntity(user), edge.Requirement));
            }

            return doAfterStarted ? SurgeryInteractionState.DoAfter : SurgeryInteractionState.Failed;
        }

        if (!surgery.Graph.TryFindNode(edge.Connection, out var newNode))
            return SurgeryInteractionState.Failed;

        CancelNodeSpecialDoAfters(surgery);
        DoNodeLeftSpecials(surgery, body, limb, user, used, bodyPart);
        surgery.CurrentNode = newNode;
        DoNodeReachedSpecials(surgery, body, limb, user, used, bodyPart);

        return SurgeryInteractionState.Passed;
    }

    private void DoNodeReachedSpecials(ISurgeryReceiver receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode?.Special is not { } specials)
            return;

        var uiUid = limb ?? body;

        foreach (var special in specials)
        {
            special.NodeReached(body, limb, user, used, bodyPart, out var ui);

            if (ui == null)
                continue;

            receiver.UserInterfaces.Add(ui);
            Ui.TryOpenUi(uiUid!.Value, ui, user);
        }
    }

    private void DoNodeLeftSpecials(ISurgeryReceiver receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode?.Special is not { } specials)
            return;

        var uiUid = limb ?? body;

        foreach (var special in specials)
        {
            special.NodeLeft(body, limb, user, used, bodyPart, out var ui);

            if (ui == null)
                continue;

            receiver.UserInterfaces.Add(ui);
            Ui.TryOpenUi(uiUid!.Value, ui, user);
        }
    }

    private bool DoInteractSpecials(ISurgeryReceiver receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode?.Special is not { } specials)
            return false;

        var netUser = GetNetEntity(user);
        var uiUid = limb ?? body;
        var handled = false;

        foreach (var special in specials)
        {
            switch (special.Interacted(body, limb, user, used, bodyPart, out var ui))
            {
                case SurgeryInteractionState.Failed:
                    continue;
                case SurgeryInteractionState.Passed:
                    handled = true;
                    continue;
                case SurgeryInteractionState.DoAfter:
                    if (!special.StartDoAfter(_doAfter, body, limb, user, used, bodyPart, out var doAfterId))
                        continue;

                    CancelSpecialDoAfter(receiver, netUser, user, special);
                    if (!receiver.SpecialDoAfters.TryGetValue(special, out var specialDoAfters))
                        specialDoAfters = [];
                    specialDoAfters[netUser] = doAfterId.Value.Index;
                    handled = true;
                    continue;
                case SurgeryInteractionState.UserInterface:
                    receiver.UserInterfaces.Add(ui!);
                    Ui.TryOpenUi(uiUid!.Value, ui!, user);
                    handled = true;
                    continue;
            }
        }

        return handled;
    }

    private void CancelEdgeRequirementDoAfters(ISurgeryReceiver surgeryReceiver)
    {
        foreach (var netDoAfter in surgeryReceiver.EdgeDoAfters.Keys)
        {
            var doAfter = new DoAfterId(GetEntity(netDoAfter.Item1), netDoAfter.Item2);
            if (!_doAfter.IsRunning(doAfter))
                continue;

            _doAfter.Cancel(doAfter);
        }

        surgeryReceiver.EdgeDoAfters.Clear();
    }

    private void CancelSpecialDoAfter(ISurgeryReceiver receiver, NetEntity netUser, EntityUid user, SurgerySpecial special)
    {
        if (!receiver.SpecialDoAfters.TryGetValue(special, out var doAfters))
            return;

        if (!doAfters.TryGetValue(netUser, out var doAfterId))
            return;

        doAfters.Remove(netUser);

        var doAfter = new DoAfterId(user, doAfterId);
        if (!_doAfter.IsRunning(doAfter))
            return;

        _doAfter.Cancel(doAfter);
    }

    private void CancelNodeSpecialDoAfters(ISurgeryReceiver receiver, SurgeryNode? node = null)
    {
        node ??= receiver.CurrentNode!;

        foreach (var special in node.Special)
        {
            if (!receiver.SpecialDoAfters.TryGetValue(special, out var specialDoAfters))
                continue;

            foreach (var (user, doAfterId) in specialDoAfters)
            {
                var doAfter = new DoAfterId(GetEntity(user), doAfterId);
                if (!_doAfter.IsRunning(doAfter))
                    continue;

                _doAfter.Cancel(doAfter);
            }
        }

        receiver.SpecialDoAfters.Clear();
    }

    private void CloseAllUis(EntityUid? uid, ISurgeryReceiver surgeryReceiver)
    {
        if (uid == null)
            return;

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
