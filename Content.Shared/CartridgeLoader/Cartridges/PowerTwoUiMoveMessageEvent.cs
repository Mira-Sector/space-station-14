using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class PowerTwoUiMoveMessageEvent(PowerTwoDirection direction) : CartridgeMessageEvent
{
    public readonly PowerTwoDirection Direction = direction;
}
