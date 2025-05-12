using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.Events;

[Serializable, NetSerializable]
public sealed partial class ModSuitDeployableGetPartEvent : HandledEntityEventArgs
{
    public NetEntity ModSuit { get; private set; }
    public string Slot { get; private set; }
    public NetEntity Part;

    public ModSuitDeployableGetPartEvent(NetEntity modSuit, string slot)
    {
        ModSuit = modSuit;
        Slot = slot;
    }
}
