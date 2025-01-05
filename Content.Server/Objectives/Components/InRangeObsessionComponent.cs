using Content.Shared.Mind;
namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class InRangeObsessionComponent : Component
{
    [DataField(required: true)]
    public string Title = string.Empty;

    [DataField]
    public string? Description;

    [DataField(required: true)]
    public uint Min;

    [DataField(required: true)]
    public uint Max;

    [ViewVariables]
    public EntityUid MindId;

    [ViewVariables]
    public MindComponent Mind = default!;

    [ViewVariables]
    public TimeSpan TimeNeeded;

    [ViewVariables]
    public TimeSpan TimeSpent;

    [ViewVariables]
    public TimeSpan NextCheck;
}
