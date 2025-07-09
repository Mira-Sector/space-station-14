using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

[Serializable, NetSerializable]
[Flags]
public enum HealthAnalyzerBodyItemBarPosition : byte
{
    Top = 1 << 0,
    Middle = 1 << 1,
    Bottom = 1 << 2,
    Left = 1 << 3,
    Center = 1 << 4,
    Right = 1 << 5,

    TopLeft = Top | Left,
    TopCenter = Top | Center,
    TopRight = Top | Right,

    MiddleLeft = Middle | Left,
    MiddleCenter = Middle | Center,
    MiddleRight = Middle | Right,

    BottomLeft = Bottom | Left,
    BottomCenter = Bottom | Center,
    BottomRight = Bottom | Right,

    X = Left | Center | Right,
    Y = Top | Middle | Bottom
}
