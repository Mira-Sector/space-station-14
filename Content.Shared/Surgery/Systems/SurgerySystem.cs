using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Standing;
using Content.Shared.Surgery.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryRecieverComponent, ComponentInit>((u, c, a) => OnLimbInit(u, c));
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, ComponentInit>((u, c, a) => OnBodyInit(u, c));

        SubscribeLocalEvent<SurgeryRecieverComponent, LimbInitEvent>((u, c, a) => OnLimbInit(u, c, a.Part));
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, BodyInitEvent>((u, c, a) => OnBodyInit(u, c));

        SubscribeLocalEvent<SurgeryRecieverBodyComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<SurgeryRecieverComponent, InteractUsingEvent>(OnLimbInteract);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, InteractUsingEvent>(OnBodyInteract);

        SubscribeLocalEvent<SurgeryRecieverComponent, SurgeryDoAfterEvent>(OnLimbDoAfter);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, SurgeryDoAfterEvent>(OnBodyDoAfter);
    }

    private void OnLimbInit(EntityUid uid, SurgeryRecieverComponent component, BodyPartComponent? bodyPartComp = null)
    {
        if (!Resolve(uid, ref bodyPartComp))
            return;

        component.Graph = MergeGraphs(component.AvailableSurgeries);
        component.Graph.TryGetStaringNode(out var startingNode);
        component.CurrentNode = startingNode;

        Dirty(uid, component);
    }

    private void OnBodyInit(EntityUid uid, SurgeryRecieverBodyComponent component)
    {
        foreach (var surgeries in component.Surgeries)
        {
            surgeries.Surgeries.Graph = MergeGraphs(surgeries.Surgeries.AvailableSurgeries);
            surgeries.Surgeries.Graph.TryGetStaringNode(out var startingNode);
            surgeries.Surgeries.CurrentNode = startingNode;
        }

        Dirty(uid, component);
    }

    private void OnBodyPartAdded(EntityUid uid, SurgeryRecieverBodyComponent component, ref BodyPartAddedEvent args)
    {
        if (!HasComp<SurgeryRecieverComponent>(args.Part))
            return;

        var netId = GetNetEntity(args.Part);

        foreach (var existingId in component.Limbs.Values)
        {
            if (netId == existingId)
                return;
        }

        component.Limbs.Add(new BodyPart(args.Part.Comp.PartType, args.Part.Comp.Symmetry), netId);
    }

    private void OnBodyPartRemoved(EntityUid uid, SurgeryRecieverBodyComponent component, ref BodyPartRemovedEvent args)
    {
        foreach (var limb in component.Limbs.Keys)
        {
            if (limb.Type != args.Part.Comp.PartType || limb.Side != args.Part.Comp.Symmetry)
                continue;

            component.Limbs.Remove(limb);
        }
    }

    private void OnLimbInteract(EntityUid uid, SurgeryRecieverComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BodyPartComponent>(uid, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        if (!PreCheck(uid))
            return;

        BodyPart bodyPart = new(bodyPartComp.PartType, bodyPartComp.Symmetry);

        args.Handled = TryTraverseGraph(uid, component, body, args.User, args.Used, bodyPart);
        Dirty(uid, component);
    }

    private void OnBodyInteract(EntityUid uid, SurgeryRecieverBodyComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DamagePartSelectorComponent>(args.User, out var damageSelectorComp))
            return;

        if (!PreCheck(uid))
            return;

        var limbFound = false;

        foreach (var (bodyPart, netLimb) in component.Limbs)
        {
            var limb = GetEntity(netLimb);

            if (!TryComp<SurgeryRecieverComponent>(limb, out var surgeryComp))
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
            TryTraverseGraph(limb, surgeryComp, uid, args.User, args.Used, bodyPart);
        }

        args.Handled = limbFound;

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

            if (TryTraverseGraph(uid, surgeries.Surgeries, uid, args.User, args.Used, surgeries.BodyPart))
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

    private void OnBodyDoAfter(EntityUid uid, SurgeryRecieverBodyComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Handled)
            return;

        foreach (var surgeries in component.Surgeries)
        {
            if (!surgeries.Surgeries.DoAfters.ContainsKey(args.DoAfter.Id))
                continue;

            if (args.Cancelled)
            {
                surgeries.Surgeries.DoAfters.Remove(args.DoAfter.Id);
                continue;
            }

            OnDoAfter(null, uid, surgeries.Surgeries, args);
        }
    }

    private void OnLimbDoAfter(EntityUid uid, SurgeryRecieverComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            if (component.DoAfters.ContainsKey(args.DoAfter.Id))
                component.DoAfters.Remove(args.DoAfter.Id);

            return;
        }

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
        CancelDoAfters(limb ?? body, surgeryReciever);
    }

    public bool TryTraverseGraph(EntityUid? limb, ISurgeryReciever surgery, EntityUid body, EntityUid user, EntityUid used, BodyPart bodyPart)
    {
        if (surgery.CurrentNode == null)
        {
            if (!surgery.Graph.TryGetStaringNode(out var startingNode))
                return false;

            surgery.CurrentNode = startingNode;
        }

        Enum? ui = null;

        foreach (var edge in surgery.CurrentNode.Edges)
        {
            // when merging the graph we made sure there arent multiple edges to traverse
            switch (TryEdge(limb, surgery, edge, body, user, used, bodyPart, out var edgeUi))
            {
                case SurgeryEdgeState.Passed:
                case SurgeryEdgeState.DoAfter:
                {
                    return true;
                }
                case SurgeryEdgeState.UserInterface:
                {
                    ui ??= edgeUi;
                    break;
                }
            }
        }

        if (ui != null)
        {
            _ui.TryOpenUi(limb ?? body, ui, user);
            return true;
        }

        return false;
    }

    public SurgeryEdgeState TryEdge(EntityUid? limb, ISurgeryReciever surgery, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid used, BodyPart bodyPart, out Enum? ui)
    {
        ui = null;

        foreach (var (doAfterId, (doAfterUser, requirement)) in surgery.DoAfters)
        {
            if (requirement != edge.Requirement)
                continue;

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
                surgery.DoAfters.Add(doAfterId.Value, (user, edge.Requirement));

            return doAfterStarted ? SurgeryEdgeState.DoAfter : SurgeryEdgeState.Failed;
        }

        if (!surgery.Graph.TryFindNode(edge.Connection, out var newNode))
            return SurgeryEdgeState.Failed;

        DoNodeLeftSpecials(surgery.CurrentNode?.Special, body, limb, user, used, bodyPart);
        surgery.CurrentNode = newNode;
        DoNodeReachedSpecials(surgery.CurrentNode?.Special, body, limb, user, used, bodyPart);

        return SurgeryEdgeState.Passed;
    }

    private void DoNodeReachedSpecials(HashSet<SurgerySpecial>? specials, EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
            special.NodeReached(body, limb, user, used, bodyPart);
    }

    private void DoNodeLeftSpecials(HashSet<SurgerySpecial>? specials, EntityUid body, EntityUid? limb, EntityUid user, EntityUid? used, BodyPart bodyPart)
    {
        if (specials == null)
            return;

        foreach (var special in specials)
            special.NodeLeft(body, limb, user, used, bodyPart);
    }

    private void CancelDoAfters(EntityUid uid, ISurgeryReciever surgeryReciever)
    {
        foreach (var doAfter in surgeryReciever.DoAfters.Keys)
        {
            if (!_doAfter.IsRunning(doAfter))
                continue;

            _doAfter.Cancel(doAfter);
        }

        surgeryReciever.DoAfters.Clear();

        foreach (var ui in surgeryReciever.UserInterfaces)
            _ui.CloseUi(uid, ui);

        surgeryReciever.UserInterfaces.Clear();
    }
}
