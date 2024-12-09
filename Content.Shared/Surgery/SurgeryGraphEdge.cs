namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphEdge
{
    [DataField("steps")]
    public SurgeryGraphStep[] _steps = Array.Empty<SurgeryGraphStep>();

    [DataField("completed")]
    public ISurgeryAction[] _completed = Array.Empty<ISurgeryAction>();

    [DataField("to", required:true)]
    public string Target { get; set; } = string.Empty;

    [ViewVariables]
    public IReadOnlyList<ISurgeryAction> Completed => _completed;

    [ViewVariables]
    public IReadOnlyList<SurgeryGraphStep> Steps => _steps;
}
