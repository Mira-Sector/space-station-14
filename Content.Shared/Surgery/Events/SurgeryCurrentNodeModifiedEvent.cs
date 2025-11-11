namespace Content.Shared.Surgery.Events;

[ByRefEvent]
public sealed partial class SurgeryCurrentNodeModifiedEvent(EntityUid receiver, EntityUid? limb, EntityUid? body, SurgeryNode previousNode, SurgeryNode currentNode, SurgeryEdge edge, SurgeryGraph graph) : BaseSurgeryCurrentNodeModifiedEvent(receiver, limb, body, previousNode, currentNode, edge, graph);
