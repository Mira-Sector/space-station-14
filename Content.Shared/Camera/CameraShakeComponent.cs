using Content.Shared.Camera.ShakeData;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraShakeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<CameraShakeEntry> Entries = [];
}

[Serializable, NetSerializable]
public struct CameraShakeEntry
{
    [ViewVariables]
    public ICameraShakeData DirectionData;

    [ViewVariables]
    public float MinMagnitude;

    [ViewVariables]
    public float MaxMagnitude;

    [ViewVariables]
    public float NoiseWeight;

    [ViewVariables]
    public TimeSpan Duration;

    [NonSerialized]
    public TimeSpan Elapsed; // no need to serialize as this constantly changes and the client can easily calculate this

    [ViewVariables]
    public float Frequency;

    [ViewVariables]
    public int Seed;
}
