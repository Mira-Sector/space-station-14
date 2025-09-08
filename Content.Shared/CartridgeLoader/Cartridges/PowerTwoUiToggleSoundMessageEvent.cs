using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed partial class PowerTwoUiToggleSoundMessageEvent(bool enable) : CartridgeMessageEvent
{
    public readonly bool Enable = enable;
}
