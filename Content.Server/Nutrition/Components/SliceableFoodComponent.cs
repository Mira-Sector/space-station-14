using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Stacks;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(SliceableFoodSystem))]
public sealed partial class SliceableFoodComponent : Component
{
    /// <summary>
    /// Prototype to spawn after slicing.
    /// If null then it can't be sliced.
    /// If the name isn't a valid prototype, crashes the game.
    /// </summary>
    [DataField]
    public EntProtoId? Slice;

    /// <summary>
    /// Prototype to spawn for a stack of items
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype>? SliceStack;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// Number of slices the food starts with.
    /// </summary>
    [DataField("count")]
    public ushort TotalCount = 5;

    /// <summary>
    /// how long it takes for this food to be sliced
    /// </summary>
    [DataField]
    public float SliceTime = 1f;

    /// <summary>
    /// all the pieces will be shifted in random directions.
    /// </summary>
    [DataField]
    public float SpawnOffset = 2.0f;

    /// <summary>
    /// additional container that will be transferred over. "food" is still always transferred over.
    /// </summary>
    [DataField]
    public string? ExtraSolution;

    /// <summary>
    /// should reagents be transferred from food to slice (true) or not (false)
    /// </summary>
    /// <remarks>
    /// note that stacked items do not transfer reagents as it doesn't work properly.
    /// </remarks>
    [DataField]
    public bool TransferReagents = true;

    /// <summary>
    /// should the number of slices be dependant on the potency of the produce (true), or static (false). If there is no potency found, defaults to false outcome.
    /// </summary>
    /// <remarks>
    /// for most plants this won't be relevent, as potency will only effect reagent amount which is already accounted for as long as reagents are transferred.
    /// would instead be relevent for plants like cotton or towercap where the sliced object itself matters more than the reagent.
    /// </remarks>
    [DataField]
    public bool PotencyEffectsCount = false;

    /// <summary>
    /// whether or not any sharp object can be used to cut this (true), or only a knife utensil (false)
    /// </summary>
    [DataField]
    public bool AnySharp = false;
}
