using Robust.Shared.GameStates;

namespace Content.Shared.Damage.DamageSelector;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamagePartAccuracyComponent : Component
{
    [DataField]
    public float MaxProb = 1f;

    [DataField]
    public float MinProb = 0.1f;

    /// <summary>
    /// If the velcocity is lower than this assume the maximum probability
    /// </summary>
    [DataField]
    public float MaxProbVelocity = 2.5f;

    /// <summary>
    /// If the velcocity is higher than this assume the minimum probability
    /// </summary>
    [DataField]
    public float MinProbVelocity = 4.5f;
}
