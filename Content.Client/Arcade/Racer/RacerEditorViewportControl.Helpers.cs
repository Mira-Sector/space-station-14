using Content.Shared.Arcade.Racer.Stage;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

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

    private void CreateNode(Vector2 position)
    {
        if (_data is not { } data)
            return;

        position = GetClosestGridPoint(position);

        var popup = new RacerEditorViewportNewNodePopup();
        popup.OnNewNodeName += args =>
        {
            if (data.Graph.Nodes.ContainsKey(args))
                return;

            var node = new RacerArcadeStageNode()
            {
                Position = position,
                Connections = []
            };
            data.Graph.Nodes[args] = node;

            // for convenience
            // you literally never want to JUST add a node
            EditNode(args, node);
        };
        AddPopup(popup);
    }

    private void EditNode(string id, RacerArcadeStageNode node)
    {
        if (_data is not { } data)
            return;

        var popup = new RacerEditorViewportEditNodePopup(id, node, data.Graph, _prototype);
        popup.OnNodeEdited += newNode =>
        {
            data.Graph.Nodes[id] = newNode;
        };
        AddPopup(popup);
    }

    private void AddPopup(RacerEditorViewportPopup popup)
    {
        if (_popup is { } oldPopup)
        {
            oldPopup.Close();
            RemoveChild(oldPopup);
        }

        _popup = popup;
        var box = UIBox2.FromDimensions(UserInterfaceManager.MousePositionScaled.Position, Vector2.One);
        popup.Open(box);
        AddChild(popup);
    }

    private void GridSizeStep(bool positive)
    {
        if (!positive && GridSize <= 0)
            return;

        var log = MathF.Log2(GridSize);
        var adjust = positive ? 1 : -1;
        var newGrid = (uint)MathF.Pow(2f, MathF.Floor(log) + adjust);
        SetGridSize(newGrid);
    }

    private Vector2 GetClosestGridPoint(Vector2 pos)
    {
        var closestX = MathF.Round(pos.X / GridSize) * GridSize;
        var closestY = MathF.Round(pos.Y / GridSize) * GridSize;
        return new Vector2(closestX, closestY);
    }
}
