using Content.Client.Silicons.Sync.Events;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Silicons.Sync.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Silicons.Sync;

public sealed partial class SiliconSyncSystem : SharedSiliconSyncSystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private SiliconSyncCommanderOverlay? _syncOverlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconSyncableSlaveAiRadialComponent, GetStationAiRadialEvent>(OnSlaveGetRadial);

        SubscribeLocalEvent<SiliconSyncableMasterCommanderComponent, ComponentInit>(OnCommanderInit);
        SubscribeLocalEvent<SiliconSyncableMasterCommanderComponent, ComponentRemove>(OnCommanderRemoved);
        SubscribeLocalEvent<SiliconSyncableMasterCommanderComponent, LocalPlayerAttachedEvent>(OnCommanderAttached);
        SubscribeLocalEvent<SiliconSyncableMasterCommanderComponent, LocalPlayerDetachedEvent>(OnCommanderDetached);

        SubscribeLocalEvent<SiliconSyncableSlaveCommandableComponent, GetStationAiRadialEvent>(OnCommandableGetRadial);
        SubscribeLocalEvent<SiliconSyncableSlaveCommandableComponent, SiliconSyncMoveSlaveGetPathSpriteEvent>(OnCommandedSprite);

        SubscribeNetworkEvent<SiliconSyncMoveSlavePathEvent>(OnGetPath);
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not {} entity)
            return;

        if (!TryComp<SiliconSyncableMasterCommanderComponent>(entity, out var commandingComp) || commandingComp.Commanding is not {} commanding)
            return;

        if (commandingComp.NextCommand > _timing.CurTime)
            return;

        commandingComp.NextCommand += CommandUpdateRate;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
            return;

        var coordinates = _transform.ToCoordinates(entity, mousePos);

        var ev = new SiliconSyncMoveSlaveToPositionEvent(GetNetCoordinates(coordinates), GetNetEntity(commanding), GetNetEntity(entity));
        RaiseNetworkEvent(ev);
    }

    private void OnSlaveGetRadial(Entity<SiliconSyncableSlaveAiRadialComponent> ent, ref GetStationAiRadialEvent args)
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

    private void OnCommanderInit(Entity<SiliconSyncableMasterCommanderComponent> ent, ref ComponentInit args)
    {
        AddOverlay();
    }

    private void OnCommanderRemoved(Entity<SiliconSyncableMasterCommanderComponent> ent, ref ComponentRemove args)
    {
        RemoveOverlay();
    }

    private void OnCommanderAttached(Entity<SiliconSyncableMasterCommanderComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    private void OnCommanderDetached(Entity<SiliconSyncableMasterCommanderComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (_syncOverlay != null)
            return;

        _syncOverlay = new SiliconSyncCommanderOverlay();
        _overlay.AddOverlay(_syncOverlay);
    }

    private void RemoveOverlay()
    {
        if (_syncOverlay == null)
            return;

        _overlay.RemoveOverlay(_syncOverlay);
        _syncOverlay = null;
    }

    private void OnCommandableGetRadial(Entity<SiliconSyncableSlaveCommandableComponent> ent, ref GetStationAiRadialEvent args)
    {
        var radial = new StationAiRadial()
        {
            Sprite = ent.Comp.Icon,
            Tooltip = Loc.GetString(ent.Comp.Tooltip),
            Event = new StationAiSyncCommandEvent()
        };

        args.Actions.Add(radial);
    }

    private void OnCommandedSprite(Entity<SiliconSyncableSlaveCommandableComponent> ent, ref SiliconSyncMoveSlaveGetPathSpriteEvent args)
    {
        args.Icon = ent.Comp.PathSprites[args.PathType];
    }

    private void OnGetPath(SiliconSyncMoveSlavePathEvent args)
    {
        if (_player.LocalEntity is not {} entity)
            return;

        var master = GetEntity(args.Master);

        if (entity != master)
            return;

        if (_syncOverlay == null)
            return;

        var slave = GetEntity(args.Slave);

        var ev = new SiliconSyncMoveSlaveGetPathSpriteEvent(args.PathType);
        RaiseLocalEvent(slave, ev);

        if (ev.Icon == null)
            return;

        if (args.PathType == SiliconSyncCommandingPathType.PathFound)
        {
            if (!_syncOverlay.Paths.TryAdd(slave, (args.Path, ev.Icon)))
                _syncOverlay.Paths[slave] = (args.Path, ev.Icon);
        }
        else
        {
            if (!_syncOverlay.Paths.TryGetValue(slave, out var path))
                return;

            _syncOverlay.Paths[slave] = (path.Item1, ev.Icon);
        }
    }
}
