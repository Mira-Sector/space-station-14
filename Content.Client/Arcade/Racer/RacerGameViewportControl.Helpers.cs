using Content.Client.PolygonRenderer;
using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.PolygonRenderer;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerGameViewportControl : PolygonRendererControl
{
    private static PolygonModel GraphToPolygonModel(
        RacerArcadeStageGraph graph,
        int samplesPerEdge,
        float maxDistance,
        Vector3 cameraPos)
    {
        List<Polygon> allPolys = [];

        foreach (var (edge, parent) in graph.GetConnections())
        {
            if (edge is not IRacerArcadeStageRenderableEdge renderableEdge)
                continue;

            if (!graph.TryGetNextNode(edge, out var nextNode))
                continue;

            var polys = BezierEdgeToPolygons(renderableEdge, parent.Position, nextNode.Position, samplesPerEdge, maxDistance, cameraPos);
            foreach (var poly in polys)
                allPolys.Add(poly);
        }

        return new PolygonModel(allPolys, Matrix4.Identity);
    }

    private static IEnumerable<Polygon> BezierEdgeToPolygons(
        IRacerArcadeStageRenderableEdge edge,
        Vector3 startNodePos,
        Vector3 endNodePos,
        int samples,
        float maxDistance,
        Vector3 cameraPos)
    {
        var worldPoints = RacerViewportControlHelpers.GetWorldSpaceEdgePoints(edge, startNodePos, endNodePos);
        var sampled = RacerViewportControlHelpers.SampleBezier(worldPoints, samples);

        var halfWidth = edge.Width * 0.5f;
        var up = Vector3.UnitZ;

        for (var i = 0; i < sampled.Length - 1; i++)
        {
            var p0 = sampled[i];
            var p1 = sampled[i + 1];

            // outside camera range
            if ((p0 - cameraPos).LengthSquared > maxDistance * maxDistance &&
                (p1 - cameraPos).LengthSquared > maxDistance * maxDistance)
                continue;

            var tangent = Vector3.Normalize(p1 - p0);

            var right = Vector3.Normalize(Vector3.Cross(up, tangent)) * halfWidth;

            var v0 = p0 - right;
            var v1 = p0 + right;
            var v2 = p1 + right;
            var v3 = p1 - right;

            yield return new FlatShadedPolygon([v0, v1, v2]);
            yield return new FlatShadedPolygon([v0, v2, v3]);
        }
    }

    private static Vector3 GetClosestPointOnTrack(
        RacerArcadeStageGraph graph,
        Vector3 playerPos,
        int samplesPerEdge)
    {
        var closest = Vector3.Zero;
        var bestDist = float.MaxValue;

        foreach (var (edge, parent) in graph.GetConnections())
        {
            if (edge is not IRacerArcadeStageRenderableEdge renderableEdge)
                continue;

            if (!graph.TryGetNextNode(edge, out var nextNode))
                continue;

            var worldPoints = RacerViewportControlHelpers.GetWorldSpaceEdgePoints(renderableEdge, parent.Position, nextNode.Position);
            var sampled = RacerViewportControlHelpers.SampleBezier(worldPoints, samplesPerEdge);

            foreach (var p in sampled)
            {
                var d = (p - playerPos).LengthSquared;
                if (d > bestDist)
                    continue;

                bestDist = d;
                closest = p;
            }
        }

        return closest;
    }
}
