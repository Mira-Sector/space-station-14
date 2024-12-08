namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphEdge
{
    [DataField("steps")]
    private SurgeryGraphStep[] _steps = Array.Empty<SurgeryGraphStep>();

    [DataField("completed", serverOnly: true)]
    private ISurgeryAction[] _completed = Array.Empty<ISurgeryAction>();

    [DataField("to", required:true)]
    public string Target { get; private set; } = string.Empty;

    [ViewVariables]
    public IReadOnlyList<ISurgeryAction> Completed => _completed;

    [ViewVariables]
    public IReadOnlyList<SurgeryGraphStep> Steps => _steps;
}
