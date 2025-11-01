using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Arcade.Racer.Stage;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl
{
    private static List<Vector2> SampleBezier(List<Vector2> points, int resolution)
    {
        List<Vector2> result = new(resolution + 1);
        for (var i = 0; i <= resolution; i++)
        {
            var t = i / (float)resolution;
            result.Add(EvaluateBezier(points, t));
        }
        return result;
    }

    private static Vector2 EvaluateBezier(List<Vector2> pts, float t)
    {
        var temp = pts.ToList();
        for (var k = pts.Count - 1; k > 0; k--)
        {
            for (var i = 0; i < k; i++)
                temp[i] = Vector2.Lerp(temp[i], temp[i + 1], t);
        }
        return temp[0];
    }

    private bool TryGetNodeAtPosition(Vector2 position, [NotNullWhen(true)] out string? nodeId, [NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        if (_data is not { } data)
        {
            nodeId = null;
            node = null;
            return false;
        }

        foreach (var (id, x) in data.Graph.Nodes)
        {
            if (Vector2.Distance(position, x.Position) > NodeRadius)
                continue;

            nodeId = id;
            node = x;
            return true;
        }

        nodeId = null;
        node = null;
        return false;
    }
}
