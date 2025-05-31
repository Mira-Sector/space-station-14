namespace Content.Client.Modules.ModSuit.Events;

public abstract partial class BaseModSuitSealedGetLayersEvent : EntityEventArgs
{
    public List<PrototypeLayerData> Layers = [];
}
