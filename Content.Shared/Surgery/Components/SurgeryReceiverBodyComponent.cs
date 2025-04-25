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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class SurgeryReceiverBodyComponent : Component
{
    /// <summary>
    /// Used for surgeries that need to be targeted but on a limb that doesn't exist
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<SurgeryBodyReceiver> Surgeries = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<BodyPart, NetEntity> Limbs = new();

    /// <summary>
    /// My sanity requires me to do this
    /// </summary>
    [ViewVariables]
    public Dictionary<NetEntity, SurgeryReceiverComponent> LimbsVV
    {
        get
        {
            Dictionary<NetEntity, SurgeryReceiverComponent> dict = new();

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


[DataDefinition, Serializable]
public sealed partial class SurgeryBodyReceiver
{
    [DataField(required: true)]
    public BodyPart BodyPart = default!;

    [DataField(required: true)]
    public SurgeryBodyPartReceiver Surgeries = new();
}

[DataDefinition, Serializable]
public sealed partial class SurgeryBodyPartReceiver : ISurgeryReceiver
{
    [DataField]
    public List<ProtoId<SurgeryPrototype>> AvailableSurgeries { get; set; } = new();

    [ViewVariables]
    public SurgeryGraph Graph { get; set; } = new();

    [ViewVariables]
    public SurgeryNode? CurrentNode { get; set; }

    [ViewVariables]
    public Dictionary<DoAfterId, (EntityUid, SurgeryEdgeRequirement)> DoAfters { get; set; } = new();

    [ViewVariables]
    public HashSet<Enum> UserInterfaces { get; set; } = new();
}
