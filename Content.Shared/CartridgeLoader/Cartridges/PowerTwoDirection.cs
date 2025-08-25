using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum PowerTwoDirection : byte
{
    Up,
    Down,
    Left,
    Right
}

[Serializable, NetSerializable]
public enum PowerTwoDirectionAxis : byte
{
    Vertical,
    Horizontal
}

public static class PowerTwoDirectionHelpers
{
    public static IEnumerable<PowerTwoDirection> GetAxisDirections(this PowerTwoDirectionAxis axis)
    {
        return axis switch
        {
            PowerTwoDirectionAxis.Vertical => [PowerTwoDirection.Up, PowerTwoDirection.Down],
            PowerTwoDirectionAxis.Horizontal => [PowerTwoDirection.Left, PowerTwoDirection.Right],
            _ => throw new NotImplementedException()
        };
    }

    public static PowerTwoDirectionAxis GetDirectionAxis(this PowerTwoDirection direction)
    {
        return direction switch
        {
            PowerTwoDirection.Up => PowerTwoDirectionAxis.Vertical,
            PowerTwoDirection.Down => PowerTwoDirectionAxis.Vertical,
            PowerTwoDirection.Left => PowerTwoDirectionAxis.Horizontal,
            PowerTwoDirection.Right => PowerTwoDirectionAxis.Horizontal,
            _ => throw new NotImplementedException()
        };
    }

    public static bool ShouldFlip(this PowerTwoDirection direction)
    {
        return direction switch
        {
            PowerTwoDirection.Up => false,
            PowerTwoDirection.Down => true,
            PowerTwoDirection.Left => false,
            PowerTwoDirection.Right => true,
            _ => throw new NotImplementedException()
        };
    }

    public static PowerTwoDirection GetPowerTwoDir(this Vector2 dir)
    {
        if (Math.Abs(dir.X) > Math.Abs(dir.Y))
            return dir.X > 0 ? PowerTwoDirection.Right : PowerTwoDirection.Left;
        else
            return dir.Y > 0 ? PowerTwoDirection.Down : PowerTwoDirection.Up;
    }
}
