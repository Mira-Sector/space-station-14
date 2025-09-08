using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class PowerTwoCartridgeSystem : SharedPowerTwoCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    protected override void UpdateUi(Entity<PowerTwoCartridgeComponent> ent, EntityUid loader)
    {
        var state = new PowerTwoUiState(ent.Comp.GameState, ent.Comp.Grid, ent.Comp.GridSize, ent.Comp.WinningScore, ent.Comp.StartTime, ent.Comp.PlaySounds);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
