using Content.Shared.IdentityManagement;
using Content.Shared.Silicons.Sync.Events;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.Sync;

public sealed class SiliconSyncSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SimpleRadialMenu? _menu;

    public SiliconSyncSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null || state is not SiliconSlaveRadialBoundUserInterfaceState cast)
            return;

        var buttonModels = ConvertToButtons(cast.Masters);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(Dictionary<NetEntity, SpriteSpecifier?> masters)
    {
        foreach (var (master, icon) in masters)
        {
            yield return new RadialMenuActionOption<NetEntity>(HandleRadialMenuClick, master)
            {
                Sprite = icon,
                ToolTip = Identity.Name(_entManager.GetEntity(master), _entManager, Owner)
            };
        }
    }

    private void HandleRadialMenuClick(NetEntity master)
    {
        var ev = new SiliconSyncSlaveMasterMessage(master);
        SendPredictedMessage(ev);
    }
}
