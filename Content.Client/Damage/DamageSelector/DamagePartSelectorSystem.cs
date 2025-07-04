using Content.Client.Alerts;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.DamageSelector;
using Robust.Client.GameObjects;

namespace Content.Client.Damage.DamageSelector;

public sealed partial class DamagePartSelectorSystem : SharedDamagePartSelectorSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string StateSuffix = "Selected";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagePartSelectorComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<DamagePartSelectorComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.Alert)
            return;

        var layer = SharedBodySystem.BodyPartToLayer(ent.Comp.SelectedPart);
        var state = $"{layer}{StateSuffix}";
        _sprite.LayerSetRsiState(args.SpriteViewEnt.AsNullable(), DamageSelectorDollLayer.Layer, state);
    }
}
