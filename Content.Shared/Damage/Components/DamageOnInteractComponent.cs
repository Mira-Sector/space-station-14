using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Components;


/// <summary>
/// This component is added to entities that you want to damage the player
/// if the player interacts with it. For example, if a player tries touching
/// a hot light bulb or an anomaly. This damage can be cancelled if the user
/// has a component that protects them from this.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnInteractComponent : Component
{
    /// <summary>
    /// How much damage to apply to the person making contact
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public bool DamageUser = true;

    /// <summary>
    /// Whether the damage should be resisted by a person's armor values
    /// and the <see cref="DamageOnInteractProtectionComponent"/>
    /// </summary>
    [DataField]
    public bool IgnoreResistances;

    [DataField]
    public bool IgnoreDamage = false;

    /// <summary>
    /// What kind of localized text should pop up when they interact with the entity
    /// </summary>
    [DataField]
    public LocId? PopupText;

    [DataField]
    public List<MobState>? RequiredStates;

    [DataField("time")]
    public float? _time;

    [ViewVariables]
    public bool UseDoAfter => _time != null;

    [ViewVariables]
    public TimeSpan DoAfterTime => TimeSpan.FromSeconds(_time ?? 0);

    [DataField]
    public bool DoAfterRepeatable = false;

    /// <summary>
    /// The sound that should be made when interacting with the entity
    /// </summary>
    [DataField]
    public SoundSpecifier InteractSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    /// <summary>
    /// Generic boolean to toggle the damage application on and off
    /// This is useful for things that can be toggled on or off, like a stovetop
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsDamageActive = true;

    /// <summary>
    /// Whether the thing should be thrown from its current position when they interact with the entity
    /// </summary>
    [DataField]
    public bool Throw = false;

    /// <summary>
    /// The speed applied to the thing when it is thrown
    /// </summary>
    [DataField]
    public int ThrowSpeed = 10;

    /// <summary>
    /// Time between being able to interact with this entity
    /// </summary>
    [DataField]
    public uint InteractTimer = 0;

    /// <summary>
    /// Tracks the last time this entity was interacted with, but only if the interaction resulted in the user taking damage
    /// </summary>
    [DataField]
    public TimeSpan LastInteraction = TimeSpan.Zero;

    /// <summary>
    /// Tracks the time that this entity can be interacted with, but only if the interaction resulted in the user taking damage
    /// </summary>
    [DataField]
    public TimeSpan NextInteraction = TimeSpan.Zero;

    /// <summary>
    /// Probability that the user will be stunned when they interact with with this entity and took damage
    /// </summary>
    [DataField]
    public float StunChance = 0.0f;

    /// <summary>
    /// Duration, in seconds, of the stun applied to the user when they interact with the entity and took damage
    /// </summary>
    [DataField]
    public float StunSeconds = 0.0f;
}

[Serializable, NetSerializable]
public sealed partial class DamageOnInteractDoAfterEvent : SimpleDoAfterEvent
{
}
