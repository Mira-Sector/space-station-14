using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[Prototype]
public sealed partial class BodyPrototype : BodyLimbChildren, IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public string Root { get; private set; } = string.Empty;

    private BodyPrototype() { }

    public BodyPrototype(string id, string name, string root, Dictionary<string, BodyPrototypeSlot> slots)
    {
        ID = id;
        Name = name;
        Root = root;
        Slots = slots;
    }
}
