using Content.Shared.Body.Part;
using Content.Shared.Surgery;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;
using System.Numerics;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryGraphControl : Control
{
    #region Constants & Config

    private const float NodeRadius = 16f;
    private const float NodeInnerRadius = 14f;
    private const float LayerHeight = 128f;
    private const float NodeSpacing = 60f;
    private const float EdgeArrowSize = 5f;
    private const float EdgeClearance = 8f;
    private const float BranchSpacing = 20f;
    private const float LayoutPadding = 20f;

    private const int BezierSegments = 24;
    private const float BezierArrowOffsetT = 0.95f;
    private const float BezierArrowTipT = 1.0f;
    private const float BackwardEdgeCurveHeight = LayerHeight / 2f;
    private const float BackwardEdgeControlOffsetX = 40f;

    private const float SelfLoopRadius = 20f;
    private const float SelfLoopYOffset = 20f;

    private const float EdgeHoverDetectionWidth = 8f;
    private const float CurvedEdgeHoverWidthMultiplier = 1.5f;
    private const float BezierEarlyRejectionMultiplier = 2f;

    private const float ScrollSensitivity = 8f;
    private const float ScrollSensitivityMultiplier = 1 / ScrollSensitivity;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 4;

    private static readonly Color NodeColor = Color.SkyBlue;
    private static readonly Color NodeHighlightColor = Color.SeaGreen;
    private static readonly Color CurrentNodeColor = Color.MediumPurple;
    private static readonly Color NodeHoverColor = Color.IndianRed;

    private static readonly Color EdgeColor = Color.PaleTurquoise;
    private static readonly Color EdgeHighlightColor = Color.GreenYellow;
    private static readonly Color EdgeHoverColor = Color.MediumVioletRed;
    private const float EdgeIconBackgroundAlpha = 0.2f;

    #endregion

    #region Dependencies

    [Dependency] private readonly IEntityManager _entity = default!;

    private readonly SpriteSystem _sprite;

    #endregion

    #region Fields

    private SurgeryGraph? _graph;
    private Dictionary<SurgeryNode, int>? _layerMap;
    private Dictionary<int, List<SurgeryNode>>? _orderedLayers;
    private Dictionary<SurgeryNode, Vector2>? _nodePositions;

    private readonly Dictionary<SurgeryEdge, Texture?> _edgeIcons = [];
    private readonly Dictionary<SurgeryNode, List<Texture>> _nodeIcons = [];

    public SurgeryNode? CurrentNode;

    private EntityUid? _receiver;
    private EntityUid? _body;
    private EntityUid? _limb;
    private BodyPart? _bodyPart;

    [ViewVariables]
    public Vector2 GraphOffset = Vector2.Zero;

    [ViewVariables]
    public Vector2 Scale = Vector2.One;

    private bool _dragging = false;

    public HashSet<SurgeryNode> HighlightedNodes = [];

    private SurgeryNode? _hoveredNode;
    private SurgeryEdge? _hoveredEdge;

    private SurgeryNode? _clickedNode;
    private SurgeryEdge? _clickedEdge;

    private Matrix3x2 Transform => Matrix3x2.CreateTranslation(GraphOffset) * Matrix3x2.CreateScale(Scale);
    private Matrix3x2 InverseTransform => Matrix3x2.Invert(Transform, out var inverse) ? inverse : Matrix3x2.Identity;

    #endregion

    #region Actions

    public event Action<SurgeryNode>? NodeClicked;
    public event Action<SurgeryEdge>? EdgeClicked;

    #endregion

    #region Initialization

    public SurgeryGraphControl() : base()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();

        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _dragging = true;
            DefaultCursorShape = CursorShape.Crosshair;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

            if (GetNodeAtPosition(graphPos) is { } node)
            {
                _clickedNode = node;
                _clickedEdge = null;
                NodeClicked?.Invoke(node);
                return;
            }

            if (GetEdgeAtPosition(graphPos) is { } edge)
            {
                _clickedNode = null;
                _clickedEdge = edge;
                EdgeClicked?.Invoke(edge);
                return;
            }
        }

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _dragging = false;
            DefaultCursorShape = CursorShape.Arrow;
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (_dragging)
        {
            GraphOffset += args.Relative / Scale;
            InvalidateMeasure();
            return;
        }

        var graphPos = Vector2.Transform(args.RelativePixelPosition, InverseTransform);

        _hoveredNode = GetNodeAtPosition(graphPos);
        _hoveredEdge = _hoveredNode == null ? GetEdgeAtPosition(graphPos) : null;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        var cursorGraphPosBeforeZoom = (args.RelativePixelPosition - GraphOffset) / Scale;

        var delta = new Vector2(args.Delta.Y, args.Delta.Y) * ScrollSensitivityMultiplier;
        Scale += delta;

        Scale = new Vector2(
            Math.Clamp(Scale.X, MinZoom, MaxZoom),
            Math.Clamp(Scale.Y, MinZoom, MaxZoom)
        );

        GraphOffset = args.RelativePosition - cursorGraphPosBeforeZoom * Scale;
        InvalidateMeasure();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (_nodePositions == null || !_nodePositions.Any())
            return Vector2.Zero;

        var bounds = Box2.Empty;

        foreach (var pos in _nodePositions.Values)
        {
            var nodeBox = Box2.FromDimensions(
                pos - new Vector2(NodeRadius),
                new Vector2(NodeRadius * 2)
            );

            bounds = bounds.Union(nodeBox);
        }

        var desiredSize = bounds.Translated(GlobalPosition).Scale(Scale).Size;

        return new Vector2(
            MathF.Min(desiredSize.X, availableSize.X),
            MathF.Min(desiredSize.Y, availableSize.Y)
        );
    }

    public void ChangeGraph(SurgeryGraph? graph, EntityUid? receiver, EntityUid? body, EntityUid? limb, BodyPart? bodyPart)
    {
        if (_graph == graph)
            return;

        _graph = graph;
        _receiver = receiver;
        _body = body;
        _limb = limb;
        _bodyPart = bodyPart;

        if (_graph == null)
        {
            _nodePositions = null;
            return;
        }

        _layerMap = AssignLayers(_graph);
        _orderedLayers = ReduceCrossings(_layerMap, _graph);
        _nodePositions = AssignCoordinates(_orderedLayers);
        InvalidateMeasure();
    }

    #endregion
}
