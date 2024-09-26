using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(HugObsessionConditionSystem))]
public sealed partial class HugObsessionConditionComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Title = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? Description;

    [DataField(required: true)]
    public uint Min;

    [DataField(required: true)]
    public uint Max;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint? HugsNeeded;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint HugsPerformed = 0;
}
