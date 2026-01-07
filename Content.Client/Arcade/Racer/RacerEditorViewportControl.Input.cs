using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl
{
    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            if (TryGetNodeAtPosition(graphPos, out _, out var node))
            {
                _dragging = false;
                _selectedNode = node;
                _selectedEdge = null;
                _selectedControlPoint = null;
                _dragOffset = node.Position.Xy - graphPos;
                return;
            }

            if (_selectedEdge is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                if (TryGetEdgeControlPointAtPosition(renderableEdge, graphPos, out var index, out var worldPos))
                {
                    _dragging = false;
                    _selectedNode = null;
                    _selectedControlPoint = index;
                    _dragOffset = worldPos - graphPos;
                    return;
                }
            }

            if (TryGetEdgeAtPosition(graphPos, out var edge, out _))
            {
                _dragging = false;
                _selectedNode = null;
                _selectedEdge = edge;
                _selectedControlPoint = null;
                _dragOffset = null;
                return;
            }

            _dragging = true;
            return;
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            if (TryGetEdgeAtPosition(graphPos, out var edge, out var nearest) && _selectedEdge == edge)
            {
                if (edge is IRacerArcadeStageRenderableEdge renderableEdge)
                {
                    AddControlPoint(renderableEdge, nearest.Value);
                    return;
                }
            }

            CreateNode(graphPos);
            return;
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            if (TryGetNodeAtPosition(graphPos, out var nodeId, out var node))
                EditNode(nodeId, node);

            return;
        }
        else if (args.Function == EngineKeyFunctions.TextDelete)
        {
            if (TryGetNodeAtPosition(graphPos, out var nodeId, out _))
                DeleteNode(nodeId);

            if (_selectedEdge is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                if (TryGetEdgeControlPointAtPosition(renderableEdge, graphPos, out var index, out _))
                    DeleteControlPoint(renderableEdge, index.Value);
            }

            return;
        }
        else if (args.Function == EngineKeyFunctions.CameraRotateLeft)
        {
            if (TryGetNodeAtPosition(graphPos, out _, out var node))
            {
                NodeHeightStep(node, false);
                return;
            }

            if (_selectedEdge is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                if (TryGetEdgeControlPointAtPosition(renderableEdge, graphPos, out var index, out _))
                    ControlPointHeightStep(renderableEdge, index.Value, false);
            }

            return;
        }
        else if (args.Function == EngineKeyFunctions.CameraRotateRight)
        {
            if (TryGetNodeAtPosition(graphPos, out _, out var node))
            {
                NodeHeightStep(node, true);
                return;
            }

            if (_selectedEdge is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                if (TryGetEdgeControlPointAtPosition(renderableEdge, graphPos, out var index, out _))
                    ControlPointHeightStep(renderableEdge, index.Value, true);
            }

            return;
        }
        else if (args.Function == EngineKeyFunctions.CameraReset)
        {
            if (TryGetNodeAtPosition(graphPos, out _, out var node))
            {
                node.Position.Z = 0f;
                return;
            }

            if (_selectedEdge is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                if (TryGetEdgeControlPointAtPosition(renderableEdge, graphPos, out var index, out _))
                    renderableEdge.ControlPoints[index.Value].Z = 0f;
            }

            return;
        }
        else if (args.Function == ContentKeyFunctions.ZoomOut)
        {
            GridSizeStep(true);
            return;
        }
        else if (args.Function == ContentKeyFunctions.ZoomIn)
        {
            GridSizeStep(false);
            return;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragging = false;
        _selectedNode = null;
        _selectedControlPoint = null;
        _dragOffset = null;
        // no disabling edge as we spawn a control for editing control points
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (_data is not { } data)
            return;

        var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

        if (_dragOffset is { } dragOffset)
        {
            var newPos = graphPos + dragOffset;
            newPos = GetClosestGridPoint(newPos);
            if (_selectedNode is { } node)
                node.Position.Xy = newPos;

            if (_selectedControlPoint is { } controlPoint && _selectedEdge is IRacerArcadeStageRenderableEdge renderableEdge)
            {
                if (data.Graph.TryGetParentNode(renderableEdge, out var parent))
                {
                    var cp = renderableEdge.ControlPoints[controlPoint];
                    var newCpLocalPos = newPos - parent.Position.Xy;
                    var newCpPos = new Vector3(newCpLocalPos.X, newCpLocalPos.Y, cp.Z);
                    renderableEdge.ControlPoints[controlPoint] = newCpPos;
                }
            }
        }

        if (_dragging)
            SetOffset(Offset + args.Relative / Scale);

        OnMousePosChanged?.Invoke(graphPos);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        var delta = new Vector2(args.Delta.Y, args.Delta.Y) * ScrollSensitivityMultiplier;
        SetScale(Scale + delta);
    }
}
