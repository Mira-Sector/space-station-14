using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Wounds;

[Serializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class WoundGraphStep
{
    [DataField]
    private IWoundAction[] _completed = Array.Empty<IWoundAction>();

    [DataField]
    public float DoAfter { get; private set; }

    public IReadOnlyList<IWoundAction> Completed => _completed;
}

[Serializable, NetSerializable]
public sealed partial class WoundInteractionDoAfterEvent : SimpleDoAfterEvent
{
}
