using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Events;

[Serializable, NetSerializable]
public sealed partial class PdaMessageClientUpdateProfilePictureEvent(NetEntity client, ProtoId<PdaChatProfilePicturePrototype> profilePicture) : EntityEventArgs, IPdaMessagePayload
{
    public NetEntity Client { get; } = client;
    public readonly ProtoId<PdaChatProfilePicturePrototype> ProfilePicture = profilePicture;

    public void RunAction(IEntityManager entity)
    {
        var client = entity.GetEntity(Client);
        entity.EventBus.RaiseLocalEvent(client, this);
    }
}
