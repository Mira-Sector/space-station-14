namespace Content.Shared.Surgery.Events;

public abstract partial class BaseSurgeryCurrentNodeModifiedEvent(EntityUid receiver, EntityUid? limb, EntityUid? body, SurgeryNode previousNode, SurgeryNode currentNode, SurgeryEdge edge, SurgeryGraph graph) : EntityEventArgs
{
    public readonly EntityUid Receiver = receiver;
    public readonly EntityUid? Limb = limb;
    public readonly EntityUid? Body = body;

    public readonly SurgeryNode PreviousNode = previousNode;
    public readonly SurgeryNode CurrentNode = currentNode;

    public readonly SurgeryEdge Edge = edge;
    public readonly SurgeryGraph Graph = graph;
}
