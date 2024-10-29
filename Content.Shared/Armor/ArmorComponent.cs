using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared.Body.Part;

namespace Content.Shared.Armor;

/// <summary>
/// Used for clothing that reduces damage when worn.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArmorSystem))]
public sealed partial class ArmorComponent : Component
{
    /// <summary>
    /// The damage reduction
    /// </summary>
    [DataField(required: true)]
    public Dictionary<List<BodyPartType>, DamageModifierSet> Modifiers = default!;

    /// <summary>
    /// If the damagereciever has no body component which damage modifier to use
    /// </summary>
    [DataField]
    public BodyPartType BasePart = BodyPartType.Torso;

    /// <summary>
    /// A multiplier applied to the calculated point value
    /// to determine the monetary value of the armor
    /// </summary>
    [DataField]
    public float PriceMultiplier = 1;
}

/// <summary>
/// Event raised on an armor entity to get additional examine text relating to its armor.
/// </summary>
/// <param name="Msg"></param>
[ByRefEvent]
public record struct ArmorExamineEvent(FormattedMessage Msg);
