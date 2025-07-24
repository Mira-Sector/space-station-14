using Content.Shared.Surgery;
using System.Linq;
using System.Numerics;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl
{
    private static Dictionary<SurgeryNode, int> AssignLayers(SurgeryGraph graph)
    {
        Dictionary<SurgeryNode, int> layers = [];
        HashSet<SurgeryNode> visited = [];

        foreach (var node in graph.Nodes.Values)
        {
            if (visited.Contains(node))
                continue;

            Queue<(SurgeryNode node, int layer)> queue = [];
            queue.Enqueue((node, 0));

            while (queue.Count > 0)
            {
                var (current, layer) = queue.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);
                layers[current] = layer;

                foreach (var edge in current.Edges)
                {
                    if (edge.Connection == null || !graph.Nodes.TryGetValue(edge.Connection.Value, out var target))
                        continue;

                    if (!visited.Contains(target))
                        queue.Enqueue((target, layer + 1));
                }
            }
        }

        return layers;
    }

    private static Dictionary<int, List<SurgeryNode>> ReduceCrossings(Dictionary<SurgeryNode, int> layers, SurgeryGraph graph)
    {
        Dictionary<int, List<SurgeryNode>> ordered = [];

        foreach (var (node, layer) in layers)
        {
            if (!ordered.ContainsKey(layer))
                ordered[layer] = [];
            ordered[layer].Add(node);
        }

        for (var i = 1; ordered.ContainsKey(i); i++)
        {
            var layer = ordered[i];
            layer.Sort((a, b) =>
            {
                float GetBarycenter(SurgeryNode node)
                {
                    List<int> indices = [];
                    foreach (var edge in node.Edges)
                    {
                        if (edge.Connection == null)
                            continue;

                        if (!graph.Nodes.TryGetValue(edge.Connection.Value, out var parent))
                            continue;

                        if (!layers.TryGetValue(parent, out var parentLayer) || parentLayer != i - 1)
                            continue;

                        var index = ordered[i - 1].IndexOf(parent);
                        if (index != -1)
                            indices.Add(index);
                    }
                    return indices.Count > 0 ? (float)indices.Average() : 0f;
                }

                return GetBarycenter(a).CompareTo(GetBarycenter(b));
            });
        }

        return ordered;
    }

    private static Dictionary<SurgeryNode, Vector2> AssignCoordinates(Dictionary<int, List<SurgeryNode>> orderedLayers)
    {
        Dictionary<SurgeryNode, Vector2> positions = [];

        foreach (var (layerIndex, nodes) in orderedLayers)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var x = LayoutPadding + i * NodeSpacing;
                var y = LayoutPadding + layerIndex * LayerHeight;
                positions[nodes[i]] = new Vector2(x, y);
            }
        }

        return positions;
    }
}
