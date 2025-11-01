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
                _dragOffset = node.Position - graphPos;
                return;
            }

            _dragging = true;
            return;
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            CreateNode(graphPos);
            return;
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            if (TryGetNodeAtPosition(graphPos, out var nodeId, out var node))
                EditNode(nodeId, node);

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
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

        if (_selectedNode is { } selected && _dragOffset is { } dragOffset)
        {
            selected.Position = graphPos + dragOffset;
        }

        if (_dragging)
            SetOffset(Offset + args.Relative / Scale);

        OnMousePosChanged?.Invoke(graphPos);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        var cursorGraphPosBeforeZoom = (args.RelativePixelPosition - Offset) / Scale;

        var delta = new Vector2(args.Delta.Y, args.Delta.Y) * ScrollSensitivityMultiplier;
        SetScale(Scale + delta);
        SetOffset(args.RelativePosition - cursorGraphPosBeforeZoom * Scale);
    }

    private void CreateNode(Vector2 position)
    {
        if (_data is not { } data)
            return;

        position = position.Rounded();

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
}
