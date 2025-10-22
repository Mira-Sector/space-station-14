using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Shadows.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedShadowSystem))]
public sealed partial class ShadowCasterComponent : Component
{
    [DataField]
    public int Radius = 8;

    [DataField]
    public float Intensity = 0.8f;

    [DataField]
    public Vector2i Offset;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, ShadowData> UnoccludedShadowMap;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, ShadowData> ShadowMap;

    [ViewVariables, AutoNetworkedField]
    public HashSet<Vector2i> PreviousOccluders = [];

    [DataField]
    public TimeSpan RecalculateDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRecalculation;

    [ViewVariables, AutoNetworkedField]
    public Vector2 LastRecalculationPos;
}
