using Content.Shared.Silicons.Sync;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.Sync;

public sealed class SiliconSyncMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SiliconSyncMonitoringWindow? _menu;

    public SiliconSyncMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SiliconSyncMonitoringWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SiliconSyncMonitoringState monitorState)
            return;

        _menu?.ShowSlaves(monitorState.MasterSlaves, monitorState.SlaveBlips, Owner);
    }
}
