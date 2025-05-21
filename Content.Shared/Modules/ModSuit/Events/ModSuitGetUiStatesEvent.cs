namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitGetUiStatesEvent : EntityEventArgs
{
    public List<BoundUserInterfaceState> States = [];
}
