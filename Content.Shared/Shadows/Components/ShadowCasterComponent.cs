using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Shadows.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedShadowSystem))]
public sealed partial class ShadowCasterComponent : Component, IComponentTreeEntry<ShadowCasterComponent>
{
    public EntityUid? TreeUid { get; set; }

    public DynamicTree<ComponentTreeEntry<ShadowCasterComponent>>? Tree { get; set; }

    public bool AddToTree => true;

    public bool TreeUpdateQueued { get; set; }

    [DataField]
    public int Radius = 8;

    [DataField]
    public float Intensity = 0.8f;

    [DataField]
    public Vector2i Offset;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, ShadowData> UnoccludedShadowMap;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, ShadowData> ShadowMap = [];

    [ViewVariables, AutoNetworkedField]
    public HashSet<Vector2i> PreviousOccluders = [];

    [DataField]
    public TimeSpan RecalculateDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRecalculation;

    [ViewVariables, AutoNetworkedField]
    public Vector2 LastRecalculationPos;
}
