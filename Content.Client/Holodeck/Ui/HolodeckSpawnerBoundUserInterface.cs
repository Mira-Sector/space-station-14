using Content.Client.UserInterface.Controls;
using Content.Shared.Holodeck.Ui;
using Robust.Client.UserInterface;

namespace Content.Client.Holodeck.Ui;

public sealed partial class HolodeckSpawnerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private HolodeckSpawnerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HolodeckSpawnerWindow>();
        _window.SetInfoFromEntity(EntMan, Owner);

        _window.OnScenarioPicked += scenario => SendMessage(new HolodeckSpawnerScenarioPickedMessage(scenario));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is HolodeckSpawnerBoundUserInterfaceState cast)
            _window?.UpdateState(Owner, cast);
    }
}
