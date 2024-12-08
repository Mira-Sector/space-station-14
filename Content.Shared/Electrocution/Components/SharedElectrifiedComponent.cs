using Robust.Shared.Audio;

namespace Content.Shared.Electrocution;

/// <summary>
///     Component for things that shock users on touch.
/// </summary>
public abstract partial class SharedElectrifiedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Should player get damage on collide
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnBump = true;

    /// <summary>
    /// Should player get damage on attack
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnAttacked = true;

    /// <summary>
    /// Should player get damage on interact with empty hand
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnHandInteract = true;

    /// <summary>
    /// Should player get damage on interact while holding an object in their hand
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnInteractUsing = true;

    [DataField, AutoNetworkedField]
    public float ShockDamage = 7.5f;

    /// <summary>
    /// Shock time, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ShockTime = 8f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier AirlockElectrifyDisabled = new("/Audio/Machines/airlock_electrify_on.ogg");

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier AirlockElectrifyEnabled = new("/Audio/Machines/airlock_electrify_off.ogg");

    [DataField, AutoNetworkedField]
    public bool PlaySoundOnShock = true;

    [DataField, AutoNetworkedField]
    public float ShockVolume = 20;

    [DataField, AutoNetworkedField]
    public float Probability = 1f;

    [DataField, AutoNetworkedField]
    public bool IsWireCut = false;
}
