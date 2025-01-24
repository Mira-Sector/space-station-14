using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Content.Shared.Surgery.Components;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryRecieverComponent, LimbInitEvent>(OnLimbInit);
        SubscribeLocalEvent<SurgeryRecieverComponent, InteractUsingEvent>(OnLimbInteract);
        SubscribeLocalEvent<SurgeryRecieverBodyComponent, InteractUsingEvent>(OnBodyInteract);
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

    public bool TryTraverseGraph(EntityUid uid, SurgeryRecieverComponent surgery, EntityUid body, EntityUid user, EntityUid used)
    {
        if (surgery.CurrentNode == null && !surgery.Graph.TryGetStaringNode(out surgery.CurrentNode))
            return false;

        foreach (var edge in surgery.CurrentNode.Edges)
        {
            // when merging the graph we make sure there arent multiple edges to traverse
            if (TryEdge(uid, surgery, edge, body, user, used))
                return true;
        }

        return false;
    }

    public bool TryEdge(EntityUid uid, SurgeryRecieverComponent surgery, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid used)
    {
        if (!RequirementsPassed(uid, surgery, edge, body, user, used))
            return false;

        DoNodeLeftSpecials(surgery.CurrentNode?.Special, body, uid);
        surgery.CurrentNode = edge.Connection;
        DotNodeReachedSpecials(surgery.CurrentNode?.Special, body, uid);

        return true;
    }

    private void DotNodeReachedSpecials(SurgerySpecial[]? specials, EntityUid body, EntityUid limb)
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

    public bool RequirementsPassed(EntityUid uid, SurgeryRecieverComponent surgery, SurgeryEdge edge, EntityUid body, EntityUid user, EntityUid used)
    {
        foreach (var requirement in edge.Requirements)
        {
            if (!requirement.RequirementMet(body, uid, user, used))
                return false;
        }

        return true;
    }
}
