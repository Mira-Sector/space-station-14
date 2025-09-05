using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Messages;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class BasePdaChatMessage
{
    [DataField]
    public PdaChatRecipientProfile Sender;

    [DataField]
    public BasePdaChatMessageable Recipient;

    [DataField]
    public TimeSpan SentAt;

    public abstract bool IsValid();
    public abstract string GetNotificationText();
    public abstract LocId GetHeaderWrapper(bool plural);
}
