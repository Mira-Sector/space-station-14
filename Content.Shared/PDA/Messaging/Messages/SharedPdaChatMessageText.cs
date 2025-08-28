using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Messages;

[Serializable, NetSerializable]
public abstract partial class SharedPdaChatMessageText : BasePdaChatMessage
{
    [DataField]
    public string Contents;

    public override string GetNotificationText() => Contents;
}
