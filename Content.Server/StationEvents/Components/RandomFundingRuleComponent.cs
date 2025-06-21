using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomFundingRule))]
public sealed partial class RandomFundingRuleComponent : Component
{
    /// <summary>
    /// Base amount of cash to pay the station
    /// </summary>
    [DataField]
    public int BaseCash = 2500;

    /// <summary>
    /// Maximum random multiplier applied to the base amount of cash
    /// </summary>
    [DataField]
    public int MaxMult = 1;

    /// <summary>
    /// Chance the funds are split across all departments as opposed to only paid to one.
    /// </summary>
    [DataField]
    public float SplitFunds = 0.3f;

    /// <summary>
    /// Can be used to override the default funding message.
    /// </summary>
    [DataField]
    public string? Message = null;
}
