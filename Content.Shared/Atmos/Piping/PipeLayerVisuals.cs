using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping;

[Serializable, NetSerializable]
public enum PipeLayerVisuals
{
    Layer
}

[Serializable, NetSerializable]
public enum PipeAppearanceLayer : byte
{
    LayerMinus2,
    LayerMinus1,
    Layer0,
    Layer1,
    Layer2
}

public class PipeAppearanceLayerHelpers
{
    public static PipeAppearanceLayer LayerToEnum(int layer)
    {
        return layer switch
        {
            -2 => PipeAppearanceLayer.LayerMinus2,
            -1 => PipeAppearanceLayer.LayerMinus1,
            0 => PipeAppearanceLayer.Layer0,
            1 => PipeAppearanceLayer.Layer1,
            2 => PipeAppearanceLayer.Layer2,
            _ => throw new NotImplementedException()
        };
    }

    public static int EnumToLayer(PipeAppearanceLayer layer)
    {
        return layer switch
        {
            PipeAppearanceLayer.LayerMinus2 => -2,
            PipeAppearanceLayer.LayerMinus1 => -1,
            PipeAppearanceLayer.Layer0 => 0,
            PipeAppearanceLayer.Layer1 => 1,
            PipeAppearanceLayer.Layer2 => 2,
            _ => throw new NotImplementedException()
        };
    }
}
