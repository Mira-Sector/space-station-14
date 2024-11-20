using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Serializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryGraphStep
{
    [DataField]
    private ISurgeryAction[] _completed = Array.Empty<ISurgeryAction>();

    [DataField]
    public float DoAfter { get; private set; }

    public IReadOnlyList<ISurgeryAction> Completed => _completed;
}

[Serializable, NetSerializable]
public sealed partial class SurgeryInteractionDoAfterEvent : SimpleDoAfterEvent
{
}
