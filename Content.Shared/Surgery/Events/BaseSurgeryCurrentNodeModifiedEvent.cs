namespace Content.Shared.Surgery.Events;

public abstract partial class BaseSurgeryCurrentNodeModifiedEvent(SurgeryNode previousNode, SurgeryNode currentNode, SurgeryEdge edge, SurgeryGraph graph) : EntityEventArgs
{
    public readonly SurgeryNode PreviousNode = previousNode;
    public readonly SurgeryNode CurrentNode = currentNode;

    public readonly SurgeryEdge Edge = edge;
    public readonly SurgeryGraph Graph = graph;
}
