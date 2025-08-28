using Content.Client.PDA.Messaging.Messages.Controls;
using Content.Shared.PDA.Messaging.Messages;
using Robust.Client.UserInterface;

namespace Content.Client.PDA.Messaging.Messages;

[DataDefinition]
public sealed partial class PdaChatMessageText : SharedPdaChatMessageText, IClientPdaChatMessage
{
    public Control GetUiControl() => new PdaChatMessageTextControl(Contents);
}
