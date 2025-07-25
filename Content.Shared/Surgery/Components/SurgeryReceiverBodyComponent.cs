using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Components;

/// <summary>
/// Gets added to the body to keep track of every limb that can get surgery
/// </summary>
/// <remarks>
/// Exists as the limbs are in a container within the body. We can never actually interact with them directly as a player.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryReceiverBodyComponent : Component
{
    /// <summary>
    /// Used for surgeries that need to be targeted but on a limb that doesn't exist
    /// </summary>
    [DataField]
    public HashSet<SurgeryBodyReceiver> Surgeries = [];

    [ViewVariables, AutoNetworkedField]
    public Dictionary<BodyPart, NetEntity> Limbs = [];

    /// <summary>
    /// My sanity requires me to do this
    /// </summary>
    [ViewVariables]
    public Dictionary<NetEntity, SurgeryReceiverComponent> LimbsVV
    {
        get
        {
            Dictionary<NetEntity, SurgeryReceiverComponent> dict = [];

            var entMan = IoCManager.Resolve<EntityManager>();

            foreach (var limb in Limbs.Values)
            {
                if (!entMan.TryGetComponent<SurgeryReceiverComponent>(entMan.GetEntity(limb), out var limbComp))
                    continue;

                dict.Add(limb, limbComp);
            }

            return dict;
        }
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SurgeryBodyReceiver
{
    [DataField(required: true)]
    public BodyPart BodyPart = default!;

    [DataField(required: true)]
    public SurgeryBodyPartReceiver Surgeries;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SurgeryBodyPartReceiver : ISurgeryReceiver
{
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; } = [];

    [ViewVariables]
    public SurgeryGraph Graph { get; set; } = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode { get; set; }

    [ViewVariables]
    public Dictionary<(NetEntity, ushort), (NetEntity, SurgeryEdgeRequirement)> EdgeDoAfters { get; set; } = [];

    [ViewVariables]
    public Dictionary<SurgerySpecial, Dictionary<NetEntity, ushort>> SpecialDoAfters { get; set; } = [];

    [ViewVariables]
    public HashSet<Enum> UserInterfaces { get; set; } = [];
}
