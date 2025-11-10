using Content.Shared.Arcade.Racer.Stage;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Numerics;

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
                _dragOffset = node.Position - graphPos;
                return;
            }

            if (TryGetEdgeAtPosition(graphPos, out var edge, out _))
            {
                _dragging = false;
                _selectedNode = null;
                _selectedEdge = edge;
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
        _dragOffset = null;
        // no disabling edge as we spawn a control for editing control points
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

        if (_selectedNode is { } selected && _dragOffset is { } dragOffset)
        {
            var newNodePos = graphPos + dragOffset;
            newNodePos = GetClosestGridPoint(newNodePos);
            selected.Position = newNodePos;
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
