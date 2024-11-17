namespace Content.Shared.Wounds;

[Serializable]
[DataDefinition]
public sealed partial class WoundGraphEdge
{
    [DataField("steps")]
    private WoundGraphStep[] _steps = Array.Empty<WoundGraphStep>();

    [DataField("completed", serverOnly: true)]
    private IWoundAction[] _completed = Array.Empty<IWoundAction>();

    [DataField("to", required:true)]
    public string Target { get; private set; } = string.Empty;

    [ViewVariables]
    public IReadOnlyList<IWoundAction> Completed => _completed;

    [ViewVariables]
    public IReadOnlyList<WoundGraphStep> Steps => _steps;
}
