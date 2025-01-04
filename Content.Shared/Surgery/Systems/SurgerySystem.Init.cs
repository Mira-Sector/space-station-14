using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Prototypes;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem
{
    private void GraphInit()
    {
        SubscribeLocalEvent<SurgeryReceiverComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SurgeryReceiverComponent component, ComponentInit args)
    {
        // been overwritten
        if (component.Graph != null)
            return;

        UpdateGraph(uid, component);
    }

    public void UpdateGraph(EntityUid uid, SurgeryReceiverComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        Dictionary<string, SurgeryGraphNode> mergedNodes = new();

        foreach (var graphID in component.AvailableSurgeries)
        {
            if (!_protoManager.TryIndex<SurgeryPrototype>(graphID, out var graph))
                continue;

            foreach (var node in graph.Nodes)
            {
                var nodeKey = graphID + node.Key;

                if (!mergedNodes.ContainsKey(nodeKey))
                    mergedNodes[nodeKey] = node.Value;

                var mergedNode = mergedNodes[nodeKey];

                foreach (var edge in node.Value.Edges)
                {
                    var edgeNodeKey = graphID + edge.Target;

                    if (!mergedNodes.ContainsKey(edgeNodeKey))
                        mergedNodes[edgeNodeKey] = graph.Nodes[edge.Target];

                    if (!mergedNode.Edges.Any(e => e.Target == edgeNodeKey && e.Completed == edge.Completed))
                    {
                        var newEdge = new SurgeryGraphEdge()
                        {
                            _steps = edge._steps,
                            _completed = edge._completed,
                            Target = edgeNodeKey,
                        };

                        mergedNode._edges.Append(newEdge);
                    }
                }
            }
        }

        component.Graph = new SurgeryGraph()
        {
            _nodes = mergedNodes,
        };
    }
}
