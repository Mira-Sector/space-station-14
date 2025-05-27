namespace Content.Client.Modules.ModSuit.Events;

public sealed partial class ModSuitSealedGetClothingLayersEvent : BaseModSuitSealedGetLayersEvent
{
    public readonly string Slot;

    public ModSuitSealedGetClothingLayersEvent(string slot)
    {
        Slot = slot;
    }
}
