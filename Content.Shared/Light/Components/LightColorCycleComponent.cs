using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LightColorCycleComponent : Component
{
    [DataField]
    public List<LightColorState> States = new();

    [ViewVariables]
    public int CurrentState;

    [DataField]
    public string UnpoweredState = "unpowered";

    [DataField]
    public bool RequirePower = false;

    [ViewVariables]
    public bool IsPowered;

    [DataField("speed")]
    public float _speed = 0.5f;

    [ViewVariables]
    public TimeSpan Speed => TimeSpan.FromSeconds(_speed);
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class LightColorState
{
    [DataField]
    public string State;

    [DataField]
    public Color Color;

    public LightColorState(string state, Color color)
    {
        State = state;
        Color = color;
    }
}

[Serializable, NetSerializable]
public enum LightColorCycleVisuals : byte
{
    State
}

public enum LightColorCycleLayers : byte
{
    Base
}
