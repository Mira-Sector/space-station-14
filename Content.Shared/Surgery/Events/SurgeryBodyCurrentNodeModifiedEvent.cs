namespace Content.Shared.Surgery.Events;

[ByRefEvent]
public sealed partial class SurgeryBodyCurrentNodeModifiedEvent(SurgeryNode previousNode, SurgeryNode currentNode, SurgeryEdge edge, SurgeryGraph graph) : BaseSurgeryCurrentNodeModifiedEvent(previousNode, currentNode, edge, graph);
