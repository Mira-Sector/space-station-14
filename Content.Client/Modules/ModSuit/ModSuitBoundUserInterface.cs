using Content.Shared.Modules.ModSuit.UI;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client.Modules.ModSuit;

[UsedImplicitly]
public sealed partial class ModSuitBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entity = default!;

    private ModSuitWindow? _window;

    public ModSuitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ModSuitWindow>();
        _window.Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ModSuitSealableBoundUserInterfaceState sealable:
                _window?.UpdateSealed(sealable);
                break;

            case ModSuitModuleBoundUserInterfaceState module:
                _window?.UpdateModules(module);
                break;

            case ModSuitComplexityBoundUserInterfaceState complexity:
                _window?.UpdateComplexity(complexity);
                break;
        }

        if (_window == null)
            return;

        _window.OnSealButtonPressed += (parts) =>
        {
            var message = new ModSuitSealButtonMessage(parts);
            SendPredictedMessage(message);
        };

        // Module buttons
        _window.OnToggleButtonPressed += (module, toggle) =>
        {
            var message = new ModSuitToggleButtonMessage(module, GetLocalEntity(), toggle);
            SendPredictedMessage(message);
        };

        _window.OnEjectButtonPressed += (module) =>
        {
            var message = new ModSuitEjectButtonMessage(module, GetLocalEntity(), GetContainer());
            SendPredictedMessage(message);
        };
    }

    internal NetEntity GetLocalEntity()
    {
        return _entity.GetNetEntity(PlayerManager.LocalEntity!.Value);
    }

    internal NetEntity GetContainer()
    {
        return _entity.GetNetEntity(Owner);
    }
}
