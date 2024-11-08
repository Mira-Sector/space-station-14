namespace Content.Shared.Wounds;

[Serializable]
[DataDefinition]
public sealed partial class WoundGraphEdge
{
    [DataField("steps")]
    private ConstructionGraphStep[] _steps = Array.Empty<ConstructionGraphStep>();

    [DataField("completed", serverOnly: true)]
    private IWoundAction[] _completed = Array.Empty<IWoundAction>();

    [DataField("to", required:true)]
    public string Target { get; private set; } = string.Empty;

    [ViewVariables]
    public IReadOnlyList<IWoundAction> Completed => _completed;

    [ViewVariables]
    public IReadOnlyList<ConstructionGraphStep> Steps => _steps;
}
