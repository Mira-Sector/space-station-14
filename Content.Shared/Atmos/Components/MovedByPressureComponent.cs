using System.Numerics;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedAtmosphereSystem))]
public sealed partial class MovedByPressureComponent : Component
{
    public const float MinPushForce = 0.1f;
    public const float MinPushForceSquared = MinPushForce * MinPushForce;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [ViewVariables, AutoNetworkedField]
    public Vector2 CurrentWind;

    [DataField]
    public float? StunForceThreshold = 4f;

    [DataField]
    public TimeSpan StunTimePerNormalizedWind = TimeSpan.FromSeconds(0.2f);
}
