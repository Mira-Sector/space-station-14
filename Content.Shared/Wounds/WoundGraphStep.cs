namespace Content.Shared.Wounds;

[Serializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ConstructionGraphStep
{
    [DataField(serverOnly: true)]
    private IWoundAction[] _completed = Array.Empty<IWoundAction>();

    [DataField]
    public float DoAfter { get; private set; }

    public IReadOnlyList<IWoundAction> Completed => _completed;
}
