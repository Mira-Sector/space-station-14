using Content.Shared.Clothing;
using Content.Shared.Instruments;

namespace Content.Server.Instruments;

public sealed partial class HeadphonesSystem : SharedHeadphonesSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadphonesComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<HeadphonesComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, HeadphonesComponent component, ClothingGotEquippedEvent args)
    {
        if (!TryComp<InstrumentComponent>(uid, out var instrument))
            return;

        Equip(uid, args.Wearer, component, instrument);
    }

    private void OnUnequipped(EntityUid uid, HeadphonesComponent component, ClothingGotUnequippedEvent args)
    {
        if (!TryComp<InstrumentComponent>(uid, out var instrument))
            return;

        Unequip(uid, component, instrument);
    }
}
