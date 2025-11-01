using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Stage;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using System.Numerics;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerEditorViewportControl : Control
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private readonly SpriteSystem _sprite;

    public Action<Vector2, Vector2>? OnGraphOffsetChanged;
    public Action<Vector2>? OnMousePosChanged;

    private RacerGameStageEditorData? _data = null;

    private RacerArcadeStageNode? _selectedNode = null;
    private Vector2? _dragOffset = null;

    private Vector2 _offset = Vector2.Zero;
    private Vector2 _scale = Vector2.One;

    [ViewVariables]
    public Vector2 Offset => _offset;

    [ViewVariables]
    public Vector2 Scale => _scale;

    private bool _dragging = false;

    private Matrix3x2 Transform => Matrix3x2.CreateTranslation(Offset) * Matrix3x2.CreateScale(Scale);
    private Matrix3x2 InverseTransform => Matrix3x2.Invert(Transform, out var inverse) ? inverse : Matrix3x2.Identity;

    private const float NodeRadius = 16f;
    private static readonly Color SelectedNodeColor = Color.Red;
    private static readonly Color NodeColor = Color.Yellow;

    private static readonly Color StandardEdgeColor = Color.Green;
    private const int RenderableEdgeBezierSamples = 32;

    private const float ScrollSensitivity = 8f;
    private const float ScrollSensitivityMultiplier = 1 / ScrollSensitivity;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 4;

    public RacerEditorViewportControl() : base()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Stop;

        _sprite = _entity.System<SpriteSystem>();
    }

    public void SetData(RacerGameStageEditorData data)
    {
        _data = data;
    }

    public void SetOffset(Vector2 offset)
    {
        if (Offset == offset)
            return;

        _offset = offset;
        InvalidateMeasure();

        OnGraphOffsetChanged?.Invoke(Offset, Scale);
    }

    public void SetScale(Vector2 scale)
    {
        if (Scale == scale)
            return;

        _scale = new Vector2(
            Math.Clamp(scale.X, MinZoom, MaxZoom),
            Math.Clamp(scale.Y, MinZoom, MaxZoom)
        );
        InvalidateMeasure();

        OnGraphOffsetChanged?.Invoke(Offset, Scale);
    }
}
