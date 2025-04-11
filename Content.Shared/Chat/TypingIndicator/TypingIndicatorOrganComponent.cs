using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorOrganComponent : Component
{
    [DataField("proto", required: true)]
    public ProtoId<TypingIndicatorPrototype> TypingIndicatorPrototype = default!;
}
