using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Silicons.Sync.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.Input;
using Robust.Shared.Map;

namespace Content.Client.Silicons.Sync;

public sealed partial class SiliconSyncSystem : SharedSiliconSyncSystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconSyncableSlaveAiRadialComponent, GetStationAiRadialEvent>(OnGetRadial);

        SubscribeLocalEvent<SiliconSyncMoveSlavePathEvent>(OnGetPath);
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not {} entity)
            return;

        if (!TryComp<SiliconSyncableMasterCommanderComponent>(entity, out var commandingComp) || commandingComp.Commanding is not {} commanding)
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
            return;

        var coordinates = _transform.ToCoordinates(entity, mousePos);

        var ev = new SiliconSyncMoveSlaveToPositionEvent(GetNetCoordinates(coordinates), GetNetEntity(commanding), GetNetEntity(entity));
        RaiseNetworkEvent(ev);
    }

    private void OnGetRadial(Entity<SiliconSyncableSlaveAiRadialComponent> ent, ref GetStationAiRadialEvent args)
    {
        if (!TryComp<SiliconSyncableSlaveComponent>(ent, out var slaveComp) || !slaveComp.Enabled || slaveComp.Master != null)
            return;

        var radial = new StationAiRadial()
        {
            Sprite = ent.Comp.Icon,
            Tooltip = Loc.GetString(ent.Comp.Tooltip),
            Event = new StationAiSyncSlaveEvent()
        };

        args.Actions.Add(radial);
    }

    private void OnGetPath(SiliconSyncMoveSlavePathEvent args)
    {
        if (_player.LocalEntity is not {} entity)
            return;

        var master = GetEntity(args.Master);
        var slave = GetEntity(args.Slave);

        if (entity != master)
            return;
    }
}
