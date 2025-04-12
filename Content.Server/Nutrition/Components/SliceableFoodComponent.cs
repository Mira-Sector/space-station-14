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
    /// </summary>
    [DataField]
    public EntProtoId? Slice;

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
    public float SpawnOffset = 0.5f;

    /// <summary>
    /// additional container that will be transferred over. "food" is still always transferred over.
    /// </summary>
    [DataField]
    public string? ExtraSolution;

    /// <summary>
    /// should reagents be transferred from food to slice
    /// </summary>
    [DataField]
    public bool TransferReagents = true;

    /// <summary>
    /// should the number of slices be dependant on the potency of the produce. If there is no potency found, does nothing.
    /// for most plants this won't be relevent, as potency will only effect reagent amount which is already accounted for
    /// would instead be relevent for plants like cotton where the sliced object itself matters more than the reagent.
    /// </summary>
    [DataField]
    public bool PotencyEffectsCount = false;
}
