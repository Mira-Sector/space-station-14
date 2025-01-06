using Content.Shared.Audio;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids;

/// <summary>
/// For entities that can clean up puddles
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbentComponent : Component
{
    [DataField]
    public string SolutionName = "absorbed";

    public Dictionary<Color, float> Progress = new();

    /// <summary>
    /// How much solution we can transfer in one interaction.
    /// </summary>
    [DataField("pickupAmount")]
    public FixedPoint2 PickupAmount = FixedPoint2.New(100);

    [DataField]
    public TimeSpan? Delay;

    [DataField]
    public string DelayId = "absorb";

    [DataField]
    public float Range = 1.2f;

    [DataField]
    public bool AlwaysPickup = false;

    [DataField]
    public bool Animation = true;

    [DataField("pickupSound")]
    public SoundSpecifier? PickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg")
    {
        Params = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation),
    };

    [DataField("transferSound")] public SoundSpecifier? TransferSound =
        new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg")
        {
            Params = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f),
        };

    public static readonly SoundSpecifier DefaultTransferSound =
        new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg")
        {
            Params = AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f),
        };
}
