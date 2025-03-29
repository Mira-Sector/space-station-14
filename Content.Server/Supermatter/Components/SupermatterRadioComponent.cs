using Content.Shared.FixedPoint;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterRadioComponent : Component
{
    [DataField]
    public Dictionary<FixedPoint2, LocId> IntegerityMessages = new();

    [DataField]
    public Dictionary<TimeSpan, LocId> CountdownMessages = new();

    [DataField]
    public ProtoId<RadioChannelPrototype> Channel;

    [ViewVariables]
    public FixedPoint2 LastIntegerityMessage;

    [ViewVariables]
    public TimeSpan LastCountdownMessage;
}
