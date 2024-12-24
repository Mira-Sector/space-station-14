using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Electrocution;

/// <summary>
///     Component for things that shock users on touch.
/// </summary>
[NetworkedComponent]
public abstract partial class SharedElectrifiedComponent : Component
{
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Should player get damage on collide
    /// </summary>
    [DataField]
    public bool OnBump = true;

    /// <summary>
    /// Should player get damage on attack
    /// </summary>
    [DataField]
    public bool OnAttacked = true;

    /// <summary>
    /// Should player get damage on interact with empty hand
    /// </summary>
    [DataField]
    public bool OnHandInteract = true;

    /// <summary>
    /// Should player get damage on interact while holding an object in their hand
    /// </summary>
    [DataField]
    public bool OnInteractUsing = true;

    [DataField]
    public float ShockDamage = 7.5f;

    /// <summary>
    /// Shock time, in seconds.
    /// </summary>
    [DataField]
    public float ShockTime = 8f;

    [DataField]
    public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    [DataField]
    public SoundPathSpecifier AirlockElectrifyDisabled = new("/Audio/Machines/airlock_electrify_on.ogg");

    [DataField]
    public SoundPathSpecifier AirlockElectrifyEnabled = new("/Audio/Machines/airlock_electrify_off.ogg");

    [DataField]
    public bool PlaySoundOnShock = true;

    [DataField]
    public float ShockVolume = 20;

    [DataField]
    public float Probability = 1f;

    [DataField]
    public bool IsWireCut = false;
}
