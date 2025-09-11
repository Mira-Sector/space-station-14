using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed partial class PowerTwoUiNewGameMessageEvent : CartridgeMessageEvent;
