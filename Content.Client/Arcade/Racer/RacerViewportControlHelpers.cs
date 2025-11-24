using System.Linq;
using Content.Shared.Arcade.Racer.Stage;

namespace Content.Client.Arcade.Racer;

public static class RacerViewportControlHelpers
{
    public static Vector3[] SampleBezier(Vector3[] points, int resolution)
    {
        var result = new Vector3[resolution + 1];
        for (var i = 0; i <= resolution; i++)
        {
            var t = i / (float)resolution;
            result[i] = EvaluateBezier(points, t);
        }
        return result;
    }

    public static Vector3 EvaluateBezier(Vector3[] pts, float t)
    {
        var temp = new Vector3[pts.Length];
        Array.Copy(pts, temp, pts.Length);
        for (var k = pts.Length - 1; k > 0; k--)
        {
            for (var i = 0; i < k; i++)
                temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
        }
        return temp[0];
    }

    public static Vector3[] GetWorldSpaceEdgePoints(IRacerArcadeStageRenderableEdge edge, Vector3 sourceNode, Vector3 nextNode)
    {
        var points = new Vector3[edge.ControlPoints.Length + 2];
        points[0] = sourceNode;

        if (edge.ControlPoints.Any())
        {
            for (var i = 0; i < edge.ControlPoints.Length; i++)
                points[i + 1] = edge.ControlPoints[i] + sourceNode;
        }
        points[^1] = nextNode;
        return points;
    }
}
