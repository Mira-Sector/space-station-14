using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Telescience.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TeleframeConsoleRadioComponent : Component
{
    /// <summary>
    /// The radio channel that that teleporation events are broadcast to
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>? AnnouncementChannel;

    [DataField]
    public bool SpeakIc;
}
