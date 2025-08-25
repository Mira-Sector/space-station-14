using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum PowerTwoGameState : byte
{
    InGame,
    GameOver,
    Won
}
