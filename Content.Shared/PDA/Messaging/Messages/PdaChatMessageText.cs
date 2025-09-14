using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Messages;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class PdaChatMessageText : BasePdaChatMessage, IRobustCloneable<PdaChatMessageText>
{
    public const int MaxSize = 128;

    [DataField]
    public string Contents;

    public override bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Contents))
            return false;

        if (Contents.Length > MaxSize)
            return false;

        return true;
    }

    public override string GetNotificationText() => FormattedMessage.RemoveMarkupPermissive(Contents);

    public override LocId GetHeaderWrapper(bool plural)
    {
        if (plural)
            return "pda-messaging-header-wrapper-text-plural";
        else
            return "pda-messaging-header-wrapper-text";
    }

    public PdaChatMessageText Clone()
    {
        return new PdaChatMessageText(this);
    }

    public PdaChatMessageText(PdaChatMessageText message) : base(message)
    {
        Contents = message.Contents;
    }
}
