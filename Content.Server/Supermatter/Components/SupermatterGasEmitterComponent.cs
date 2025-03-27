using Content.Server.Supermatter.GasReactions;
using Content.Shared.Atmos;
using System.Collections.Immutable;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterGasEmitterComponent : Component
{
    [DataField]
    public Dictionary<Gas, float> Ratios = new();

    [ViewVariables]
    public float CurrentRate
    {
        get => CurrentRate;
        set => Math.Max(value, MinRate);
    }

    [DataField]
    public float MinRate;

    [ViewVariables]
    public float CurrentTemperature
    {
        get => CurrentRate;
        set => Math.Max(value, MinTemperature);
    }

    [DataField]
    public float MinTemperature;

    [DataField]
    public TimeSpan Delay;

    [ViewVariables]
    public TimeSpan NextSpawn;

    /// <summary>
    /// How much of each gas from last time we emitted gas
    /// </summary>
    /// <remarks>
    /// We need to keep track of this so we can reverse a gas prior reaction
    /// </remarks>
    [ViewVariables]
    public Dictionary<Gas, float> PreviousPercentage = new();

    /// <summary>
    /// What reactions can modify our data
    /// </summary>
    /// <remarks>
    /// Used to cleanup <see cref= PreviousPercentage>
    /// </remarks>
    public readonly ImmutableArray<Type> ModifiableReactions = new()
    {
        typeof(ModifyWaste)
    };
}
