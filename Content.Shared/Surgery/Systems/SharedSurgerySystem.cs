using System.Linq;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
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
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem Ui = default!;

    private EntityQuery<SurgeryReceiverComponent> _receiverQuery;
    private EntityQuery<BodyPartComponent> _bodyPartQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        _receiverQuery = GetEntityQuery<SurgeryReceiverComponent>();
        _bodyPartQuery = GetEntityQuery<BodyPartComponent>();

        SubscribeLocalEvent<SurgeryReceiverComponent, ComponentInit>((u, c, a) => OnLimbInit(u, c));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, ComponentInit>((u, c, a) => OnBodyInit(u, c));

        SubscribeLocalEvent<SurgeryReceiverComponent, LimbInitEvent>((u, c, a) => OnLimbInit(u, c));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, BodyInitEvent>((u, c, a) => OnBodyInit(u, c));

        SubscribeLocalEvent<SurgeryReceiverBodyComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<SurgeryReceiverComponent, InteractUsingEvent>((u, c, a) => OnLimbInteract(u, c, a.User, a.Used, a));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, InteractUsingEvent>((u, c, a) => OnBodyInteract(u, c, a.User, a.Used, a));

        SubscribeLocalEvent<SurgeryReceiverComponent, InteractHandEvent>((u, c, a) => OnLimbInteract(u, c, a.User, null, a));
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, InteractHandEvent>((u, c, a) => OnBodyInteract(u, c, a.User, null, a));

        SubscribeLocalEvent<SurgeryReceiverComponent, SurgeryEdgeRequirementDoAfterEvent>(OnLimbEdgeRequirementDoAfter);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, SurgeryEdgeRequirementDoAfterEvent>(OnBodyEdgeRequirementDoAfter);

        SubscribeLocalEvent<SurgeryReceiverComponent, SurgerySpecialDoAfterEvent>(OnLimbSpecialDoAfter);
        SubscribeLocalEvent<SurgeryReceiverBodyComponent, SurgerySpecialDoAfterEvent>(OnBodySpecialDoAfter);

        SubscribeLocalEvent<AllowOrganSurgeryComponent, ComponentInit>(OnOrganSurgeryInit);
        SubscribeLocalEvent<AllowOrganSurgeryComponent, OrganAddedLimbEvent>(OnOrganSurgeryOrganAdded);
        SubscribeLocalEvent<AllowOrganSurgeryComponent, OrganRemovedLimbEvent>(OnOrganSurgeryOrganRemoved);
    }

    private void OnLimbInit(EntityUid uid, SurgeryReceiverComponent component)
    {
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

        if (!_bodyPartQuery.TryComp(uid, out var bodyPartComp))
            return;

        BodyPart bodyPart = new(bodyPartComp.PartType, bodyPartComp.Symmetry);

        if (!PreCheck(uid, component, uid, bodyPartComp.Body, used, user, bodyPart))
            return;

        if (DoInteractSpecials(SurgerySpecialInteractionPhase.BeforeGraph, uid, component, bodyPartComp.Body, uid, user, used, bodyPart))
        {
            args.Handled = true;
            return;
        }

        if (TryTraverseGraph(uid, component, uid, bodyPartComp.Body, user, used, bodyPart))
        {
            Dirty(uid, component);
            args.Handled = true;
            return;
        }

        if (DoInteractSpecials(SurgerySpecialInteractionPhase.AfterGraph, uid, component, bodyPartComp.Body, uid, user, used, bodyPart))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<AllowOrganSurgeryComponent>(uid, out var allowOrganSurgery))
            return;

        foreach (var organ in allowOrganSurgery.Organs)
        {
            var receiver = _receiverQuery.GetComponent(organ);

            if (!PreCheck(organ, receiver, uid, bodyPartComp.Body, used, user, bodyPart))
                return;

            if (DoInteractSpecials(SurgerySpecialInteractionPhase.BeforeGraph, organ, receiver, bodyPartComp.Body, uid, user, used, bodyPart))
            {
                args.Handled = true;
                return;
            }

            if (TryTraverseGraph(organ, receiver, uid, bodyPartComp.Body, user, used, bodyPart))
            {
                Dirty(organ, receiver);
                args.Handled = true;
                return;
            }

            if (DoInteractSpecials(SurgerySpecialInteractionPhase.AfterGraph, organ, receiver, bodyPartComp.Body, uid, user, used, bodyPart))
            {
                args.Handled = true;
                return;
            }
        }
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

            if (!_receiverQuery.TryComp(limb, out var surgeryComp))
                continue;

            if (!_bodyPartQuery.TryComp(limb, out var partComp) || partComp.Body != uid)
                continue;

            if (bodyPart.Type != damageSelectorComp.SelectedPart.Type)
                continue;

            if (bodyPart.Side != damageSelectorComp.SelectedPart.Side)
                continue;

            // we have a limb we can do surgery on
            limbFound = true;

            if (!PreCheck(limb, surgeryComp, limb, uid, used, user, damageSelectorComp.SelectedPart))
                return;

            if (DoInteractSpecials(SurgerySpecialInteractionPhase.BeforeGraph, limb, surgeryComp, uid, limb, user, used, bodyPart))
            {
                args.Handled = true;
                return;
            }

            // may have multiple limbs so dont exit early
            if (TryTraverseGraph(limb, surgeryComp, limb, uid, user, used, bodyPart))
            {
                Dirty(limb, surgeryComp);
                args.Handled = true;
                return;
            }

            if (DoInteractSpecials(SurgerySpecialInteractionPhase.AfterGraph, limb, surgeryComp, uid, limb, user, used, bodyPart))
            {
                args.Handled = true;
                return;
            }

            if (!TryComp<AllowOrganSurgeryComponent>(uid, out var allowOrganSurgery))
                continue;

            foreach (var organ in allowOrganSurgery.Organs)
            {
                if (!_receiverQuery.TryComp(organ, out var organReceiver))
                    continue;

                if (!PreCheck(organ, organReceiver, limb, uid, used, user, damageSelectorComp.SelectedPart))
                    return;

                if (DoInteractSpecials(SurgerySpecialInteractionPhase.BeforeGraph, organ, surgeryComp, uid, limb, user, used, bodyPart))
                {
                    args.Handled = true;
                    return;
                }

                if (TryTraverseGraph(organ, organReceiver, limb, uid, user, used, bodyPart))
                {
                    Dirty(organ, organReceiver);
                    args.Handled = true;
                    return;
                }

                if (DoInteractSpecials(SurgerySpecialInteractionPhase.AfterGraph, organ, organReceiver, uid, limb, user, used, bodyPart))
                {
                    args.Handled = true;
                    return;
                }
            }
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

            if (!PreCheck(uid, surgeries.Surgeries, null, uid, used, user, damageSelectorComp.SelectedPart))
                return;

            if (DoInteractSpecials(SurgerySpecialInteractionPhase.BeforeGraph, uid, surgeries.Surgeries, uid, null, user, used, surgeries.BodyPart))
            {
                args.Handled = true;
                return;
            }

            if (TryTraverseGraph(uid, surgeries.Surgeries, null, uid, user, used, surgeries.BodyPart))
            {
                Dirty(uid, component);
                args.Handled = true;
                return;
            }

            args.Handled |= DoInteractSpecials(SurgerySpecialInteractionPhase.AfterGraph, uid, surgeries.Surgeries, uid, null, user, used, surgeries.BodyPart);
            return;
        }
    }

    private bool PreCheck(EntityUid receiverUid, ISurgeryReceiver receiver, EntityUid? limb, EntityUid? body, EntityUid? used, EntityUid user, BodyPart bodyPart)
    {
        var ev = new SurgeryInteractionAttemptEvent(body, limb, used, user, bodyPart);
        RaiseLocalEvent(receiverUid, ref ev);
        if (limb != null && receiverUid != limb.Value)
            RaiseLocalEvent(limb.Value, ref ev);
        if (body != null)
            RaiseLocalEvent(body.Value, ref ev);

        if (!ev.Cancelled || ev.Reason == null || receiver.CurrentNode is not { } currentNode)
            return !ev.Cancelled;

        // check if we could have even progressed
        // if not dont show the reason to stop irrelevant messages showing
        foreach (var edge in currentNode.Edges)
        {
            if (edge.Requirement.RequirementMet(receiverUid, body, limb, user, used, bodyPart, out _, true) == SurgeryInteractionState.Failed)
                continue;

            _popup.PopupPredicted(ev.Reason, receiverUid, user);
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

            OnEdgeRequirementDoAfter(uid, null, uid, surgeries.Surgeries, args);
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

        if (!_bodyPartQuery.TryComp(uid, out var bodyPartComp))
            return;

        if (args.BodyPart.Type != bodyPartComp.PartType || args.BodyPart.Side != bodyPartComp.Symmetry)
            return;

        OnEdgeRequirementDoAfter(uid, uid, bodyPartComp.Body, component, args);
    }

    private void OnEdgeRequirementDoAfter(EntityUid receiverUid, EntityUid? limb, EntityUid? body, ISurgeryReceiver surgeryReceiver, SurgeryEdgeRequirementDoAfterEvent args)
    {
        if (!surgeryReceiver.Graph.TryFindNode(args.TargetEdge.Connection, out var newNode))
            return;

        args.Handled = true;

        var oldNode = surgeryReceiver.CurrentNode;

        DoNodeLeftSpecials(receiverUid, surgeryReceiver, body, limb, args.User, args.Used, args.BodyPart);
        surgeryReceiver.CurrentNode = newNode;
        DoNodeReachedSpecials(receiverUid, surgeryReceiver, body, limb, args.User, args.Used, args.BodyPart);

        var netDoAfterId = (GetNetEntity(args.DoAfter.Id.Uid), args.DoAfter.Id.Index);
        surgeryReceiver.EdgeDoAfters.Remove(netDoAfterId);
        CancelEdgeRequirementDoAfters(surgeryReceiver);
        CancelNodeSpecialDoAfters(surgeryReceiver, oldNode);
        CloseAllUis(limb ?? body, surgeryReceiver);

        TryDealPain(args.TargetEdge.Requirement.Pain, receiverUid, body, limb, args.Used);

        RaiseNodeModifiedEvents(receiverUid, limb, body, surgeryReceiver, oldNode!, newNode, args.TargetEdge);
    }

    private void OnLimbSpecialDoAfter(EntityUid uid, SurgeryReceiverComponent component, SurgerySpecialDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            var netUser = GetNetEntity(args.User);
            CancelSpecialDoAfter(component, netUser, args.User, args.Special);
            return;
        }

        if (!_bodyPartQuery.TryComp(uid, out var bodyPartComp))
            return;

        if (args.BodyPart.Type != bodyPartComp.PartType || args.BodyPart.Side != bodyPartComp.Symmetry)
            return;

        OnSpecialDoAfter(uid, uid, bodyPartComp.Body, args);
    }

    private void OnBodySpecialDoAfter(EntityUid uid, SurgeryReceiverBodyComponent component, SurgerySpecialDoAfterEvent args)
    {
        if (args.Handled)
            return;

        foreach (var surgeries in component.Surgeries)
        {
            if (args.BodyPart.Type != surgeries.BodyPart.Type || args.BodyPart.Side != surgeries.BodyPart.Side)
                continue;

            if (args.Cancelled)
                CancelSpecialDoAfter(surgeries.Surgeries, GetNetEntity(args.User), args.User, args.Special);
            else
                OnSpecialDoAfter(uid, null, uid, args);

            break;
        }
    }

    private static void OnSpecialDoAfter(EntityUid receiver, EntityUid? limb, EntityUid? body, SurgerySpecialDoAfterEvent args)
    {
        args.Handled = true;
        args.Special.OnDoAfter(receiver, body, limb, args);
    }

    private void OnOrganSurgeryInit(Entity<AllowOrganSurgeryComponent> ent, ref ComponentInit args)
    {
        var organs = _body.GetPartOrgans(ent.Owner);

        ent.Comp.Organs.EnsureCapacity(organs.Count());

        foreach (var (organ, _) in organs)
        {
            if (_receiverQuery.HasComp(organ))
                ent.Comp.Organs.Add(organ);
        }

        Dirty(ent);
    }

    private void OnOrganSurgeryOrganAdded(Entity<AllowOrganSurgeryComponent> ent, ref OrganAddedLimbEvent args)
    {
        if (!_receiverQuery.HasComp(args.Organ))
            return;

        ent.Comp.Organs.Add(args.Organ);
        Dirty(ent);

        if (!TryGetUiEntity(ent.Owner, out var ui))
            return;

        UpdateUi(ui.Value, ent.Owner);
    }

    private void OnOrganSurgeryOrganRemoved(Entity<AllowOrganSurgeryComponent> ent, ref OrganRemovedLimbEvent args)
    {
        if (!ent.Comp.Organs.Remove(args.Organ))
            return;

        Dirty(ent);

        if (!TryGetUiEntity(ent.Owner, out var ui))
            return;

        UpdateUi(ui.Value, ent.Owner);
    }


    public bool TryTraverseGraph(EntityUid receiverUid, ISurgeryReceiver receiver, EntityUid? limb, EntityUid? body, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode == null)
        {
            if (!receiver.Graph.TryGetStaringNode(out var startingNode))
                return false;

            receiver.CurrentNode = startingNode;
        }

        Enum? ui = null;

        var oldNode = receiver.CurrentNode;

        foreach (var edge in receiver.CurrentNode.Edges)
        {
            // when merging the graph we made sure there arent multiple edges to traverse
            switch (TryEdge(receiverUid, receiver, edge, limb, body, user, used, bodyPart, out var edgeUi))
            {
                case SurgeryInteractionState.Passed:
                    TryDealPain(edge.Requirement.Pain, receiverUid, body, limb, used);
                    RaiseNodeModifiedEvents(receiverUid, limb, body, receiver, oldNode!, receiver.CurrentNode, edge);
                    return true;
                case SurgeryInteractionState.DoAfter:
                    return true;
                case SurgeryInteractionState.UserInterface:
                    ui ??= edgeUi;
                    TryDealPain(edge.Requirement.Pain, receiverUid, body, limb, used);
                    RaiseNodeModifiedEvents(receiverUid, limb, body, receiver, oldNode!, receiver.CurrentNode, edge);
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

    public SurgeryInteractionState TryEdge(EntityUid receiverUid, ISurgeryReceiver receiver, SurgeryEdge edge, EntityUid? limb, EntityUid? body, EntityUid user, EntityUid? used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        foreach (var ((doAfterNetUid, doAfterIndex), (doAfterNetUser, requirement)) in receiver.EdgeDoAfters)
        {
            var doAfterId = new DoAfterId(GetEntity(doAfterNetUid), doAfterIndex);

            if (_doAfter.GetStatus(doAfterId) != DoAfterStatus.Running)
                continue;

            if (requirement != edge.Requirement)
                continue;

            var doAfterUser = GetEntity(doAfterNetUser);

            if (doAfterUser != user)
                continue;

            _doAfter.Cancel(doAfterId);
            return SurgeryInteractionState.DoAfter;
        }

        var requirementsPassed = edge.Requirement.RequirementMet(receiverUid, body, limb, user, used, bodyPart, out ui);

        if (requirementsPassed == SurgeryInteractionState.Failed)
            return SurgeryInteractionState.Failed;

        if (requirementsPassed == SurgeryInteractionState.UserInterface)
        {
            if (ui == null)
                return SurgeryInteractionState.Failed;

            receiver.UserInterfaces.Add(ui);

            return SurgeryInteractionState.UserInterface;
        }

        CancelEdgeRequirementDoAfters(receiver);
        CloseAllUis(receiverUid, receiver);

        if (requirementsPassed == SurgeryInteractionState.DoAfter)
        {
            var doAfterStarted = edge.Requirement.StartDoAfter(_doAfter, edge, receiverUid, body, limb, user, used, bodyPart, out var doAfterId);

            if (doAfterId != null)
            {
                var netDoAfterId = (GetNetEntity(doAfterId.Value.Uid), doAfterId.Value.Index);
                receiver.EdgeDoAfters.Add(netDoAfterId, (GetNetEntity(user), edge.Requirement));
            }

            return doAfterStarted ? SurgeryInteractionState.DoAfter : SurgeryInteractionState.Failed;
        }

        if (!receiver.Graph.TryFindNode(edge.Connection, out var newNode))
            return SurgeryInteractionState.Failed;

        CancelNodeSpecialDoAfters(receiver);
        DoNodeLeftSpecials(receiverUid, receiver, body, limb, user, used, bodyPart);
        receiver.CurrentNode = newNode;
        DoNodeReachedSpecials(receiverUid, receiver, body, limb, user, used, bodyPart);

        return SurgeryInteractionState.Passed;
    }

    private void DoNodeReachedSpecials(EntityUid receiverUid, ISurgeryReceiver receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode?.Special is not { } specials)
            return;

        foreach (var special in specials)
        {
            special.NodeReached(receiverUid, body, limb, user, used, bodyPart, out var ui, out var bodyUi);

            if (ui == null)
                continue;

            var uiUid = bodyUi ? body : receiverUid;
            if (uiUid == null)
                continue;

            Ui.TryOpenUi(uiUid.Value, ui, user);
            receiver.UserInterfaces.Add(ui);
        }
    }

    private void DoNodeLeftSpecials(EntityUid receiverUid, ISurgeryReceiver receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode?.Special is not { } specials)
            return;

        foreach (var special in specials)
        {
            special.NodeLeft(receiverUid, body, limb, user, used, bodyPart, out var ui, out var bodyUi);

            if (ui == null)
                continue;

            var uiUid = bodyUi ? body : receiverUid;
            if (uiUid == null)
                continue;

            Ui.TryOpenUi(uiUid.Value, ui, user);
            receiver.UserInterfaces.Add(ui);
        }
    }

    private bool DoInteractSpecials(SurgerySpecialInteractionPhase phase, EntityUid receiverUid, ISurgeryReceiver receiver, EntityUid? body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (receiver.CurrentNode?.Special is not { } specials)
            return false;

        var netUser = GetNetEntity(user);
        var handled = false;

        foreach (var special in specials)
        {
            switch (special.Interacted(phase, receiverUid, body, limb, user, used, bodyPart, out var ui, out var bodyUi))
            {
                case SurgeryInteractionState.Failed:
                    continue;
                case SurgeryInteractionState.Passed:
                    handled = true;
                    continue;
                case SurgeryInteractionState.DoAfter:
                    if (!special.StartDoAfter(_doAfter, receiverUid, body, limb, user, used, bodyPart, out var doAfterId))
                        continue;

                    CancelSpecialDoAfter(receiver, netUser, user, special);
                    if (!receiver.SpecialDoAfters.TryGetValue(special, out var specialDoAfters))
                        specialDoAfters = [];
                    specialDoAfters[netUser] = doAfterId.Value.Index;
                    handled = true;
                    continue;
                case SurgeryInteractionState.UserInterface:
                    var uiUid = bodyUi ? body : receiverUid;
                    if (uiUid == null)
                        continue;

                    receiver.UserInterfaces.Add(ui!);
                    Ui.TryOpenUi(uiUid.Value, ui!, user);
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

    private void RaiseNodeModifiedEvents(EntityUid receiverUid, EntityUid? limb, EntityUid? body, ISurgeryReceiver receiver, SurgeryNode previousNode, SurgeryNode currentNode, SurgeryEdge edge)
    {
        var receiverEv = new SurgeryCurrentNodeModifiedEvent(previousNode, currentNode, edge, receiver.Graph);
        RaiseLocalEvent(receiverUid, ref receiverEv);

        if (limb != null && receiverUid != limb.Value)
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
