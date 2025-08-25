using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class PowerTwoCartridgeSystem : SharedPowerTwoCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerTwoCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(Entity<PowerTwoCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        NewGame(ent);

        UpdateUi(ent, args.Loader);
    }

    protected override void UpdateUi(Entity<PowerTwoCartridgeComponent> ent, EntityUid loader)
    {
        var state = new PowerTwoUiState(ent.Comp.GameState, ent.Comp.Grid, ent.Comp.GridSize, ent.Comp.WinningScore, ent.Comp.StartTime);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
