using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

/// <summary>
/// Gets added to the body to keep track of every limb that can get surgery
/// </summary>
/// <remarks>
/// Exists as the limbs are in a container within the body. We can never actually interact with them directly as a player.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryRecieverBodyComponent : Component
{
    /// <summary>
    /// Used for surgeries that need to be targeted but on a limb that doesn't exist
    /// </summary>
    [DataField]
    public HashSet<SurgeryBodyReciever> Surgeries = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<BodyPart, NetEntity> Limbs = new();
}


[DataDefinition]
public sealed partial class SurgeryBodyReciever
{
    [DataField(required: true)]
    public BodyPart BodyPart = default!;

    [DataField(required: true)]
    public SurgeryBodyPartReciever Surgeries = new();
}

[DataDefinition]
public sealed partial class SurgeryBodyPartReciever : ISurgeryReciever
{
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; } = new();

    [ViewVariables]
    public SurgeryGraph Graph { get; set; } = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode { get; set; }

    [ViewVariables]
    public List<DoAfterId> DoAfters { get; set; } = new();
}
