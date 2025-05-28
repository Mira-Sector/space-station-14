using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Prototypes;

[Serializable, NetSerializable]
[DataDefinition, Virtual]
public partial class BodyLimbChildren
{
    [DataField]
    public Dictionary<string, BodyPrototypeSlot> Slots { get; protected set; } = new();

    [DataField]
    public string Root { get; protected set; } = string.Empty;
}

[DataRecord, Serializable, NetSerializable]
public sealed record BodyPrototypeSlot(EntProtoId? Part, HashSet<string> Connections, Dictionary<string, string> Organs);
