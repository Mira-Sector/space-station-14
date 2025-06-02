using System.Numerics;
using Content.Shared.Intents;
using Content.Shared.Weapons.Melee;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Melee;

/// <summary>
/// Debug overlay showing the arc and range of a melee weapon.
/// </summary>
public sealed class MeleeArcOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IEyeManager _eyeManager;
    private readonly IInputManager _inputManager;
    private readonly IPlayerManager _playerManager;
    private readonly MeleeWeaponSystem _melee;
    private readonly IntentSystem _intent;
    private readonly SharedTransformSystem _transform = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private static readonly ProtoId<IntentPrototype> HarmIntent = "Harm";

    public MeleeArcOverlay(IEntityManager entManager, IEyeManager eyeManager, IInputManager inputManager, IPlayerManager playerManager, MeleeWeaponSystem melee, IntentSystem intent, SharedTransformSystem transform)
    {
        _entManager = entManager;
        _eyeManager = eyeManager;
        _inputManager = inputManager;
        _playerManager = playerManager;
        _melee = melee;
        _intent = intent;
        _transform = transform;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalEntity;

        if (!_entManager.TryGetComponent<TransformComponent>(player, out var xform) ||
            !_intent.TryGetIntent(player.Value, out var intent)
            || intent != HarmIntent)
        {
            return;
        }

        if (!_melee.TryGetWeapon(player.Value, out _, out var weapon))
            return;

        var mousePos = _inputManager.MouseScreenPosition;
        var mapPos = _eyeManager.PixelToMap(mousePos);

        if (mapPos.MapId != args.MapId)
            return;

        var playerPos = _transform.GetMapCoordinates(player.Value, xform: xform);

        if (mapPos.MapId != playerPos.MapId)
            return;

        var diff = mapPos.Position - playerPos.Position;

        if (diff.Equals(Vector2.Zero))
            return;

        diff = diff.Normalized() * Math.Min(weapon.Range, diff.Length());
        args.WorldHandle.DrawLine(playerPos.Position, playerPos.Position + diff, Color.Aqua);

        if (weapon.Angle.Theta == 0)
            return;

        args.WorldHandle.DrawLine(playerPos.Position, playerPos.Position + new Angle(-weapon.Angle / 2).RotateVec(diff), Color.Orange);
        args.WorldHandle.DrawLine(playerPos.Position, playerPos.Position + new Angle(weapon.Angle / 2).RotateVec(diff), Color.Orange);
    }
}
