using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Messages;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class PdaChatMessageText : BasePdaChatMessage
{
    [DataField]
    public string Contents;

    public override string GetNotificationText() => Contents;

    public override LocId GetHeaderWrapper(bool plural)
    {
        if (plural)
            return "pda-messaging-header-wrapper-text-plural";
        else
            return "pda-messaging-header-wrapper-text";
    }
}
