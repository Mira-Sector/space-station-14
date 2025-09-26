using Robust.Shared.Serialization;

namespace Content.Shared.Telescience;

[Serializable, NetSerializable]
public enum TeleframeActivationMode : byte
{
    Send,
    Receive
}

public static class TeleframeActivationModeHelpers
{
    public static TeleframeActivationMode GetOpposite(this TeleframeActivationMode mode)
    {
        return mode switch
        {
            TeleframeActivationMode.Receive => TeleframeActivationMode.Send,
            TeleframeActivationMode.Send => TeleframeActivationMode.Receive,
            _ => throw new NotImplementedException()
        };
    }
}
