using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly StoreSystem _store = default!;

    private void InitializeHacking()
    {
        SubscribeLocalEvent<StationAiCanHackComponent, StationAiShopActionEvent>(OnShop);
        SubscribeLocalEvent<StationAiHackableComponent, StationAiHackEvent>((u, c, e) => OnHack(u, c, e.User));
    }

    private void OnShop(EntityUid uid, StationAiCanHackComponent component, StationAiShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    private void OnHack(EntityUid uid, StationAiHackableComponent component, EntityUid user)
    {
        if (!component.Enabled || !component.IsPowered || component.Hacked)
            return;

        if (!TryUpdateAiPower(user, component.Points))
            return;

        component.Hacked = true;
        Dirty(uid, component);
    }

    public bool TryUpdateAiPower(EntityUid uid, FixedPoint2 power, StationAiCanHackComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (!_store.TryAddCurrency(new Dictionary<string, FixedPoint2> { {component.CurrencyPrototype, power} }, uid))
            return false;

        return true;
    }
}
