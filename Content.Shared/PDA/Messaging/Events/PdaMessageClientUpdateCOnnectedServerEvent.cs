using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Messaging.Events;

[Serializable, NetSerializable]
public sealed partial class PdaMessageClientUpdateConnectedServerEvent(NetEntity client, NetEntity? server) : EntityEventArgs, IPdaMessagePayload
{
    public NetEntity Client { get; } = client;
    public readonly NetEntity? Server = server;
}
