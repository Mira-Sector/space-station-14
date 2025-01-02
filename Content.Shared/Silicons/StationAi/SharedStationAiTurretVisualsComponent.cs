using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

[NetworkedComponent]
public abstract partial class SharedStationAiTurretVisualsComponent : Component
{
    [ViewVariables]
    public TurretState CurrentState;

    [ViewVariables]
    public TimeSpan LastUpdate;

    [ViewVariables]
    public TimeSpan OpeningTime;

    [ViewVariables]
    public TimeSpan ClosingTime;
}

[Serializable, NetSerializable]
public enum TurretState : byte
{
    Open,
    Closed,
    Opening,
    Closing
}

[Serializable, NetSerializable]
public enum TurretVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum TurretVisualLayers : byte
{
    Base,
    Turret
}
