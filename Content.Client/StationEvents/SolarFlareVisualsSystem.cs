using System.Linq;
using Content.Shared.StationEvents.Events;
using Robust.Client.Graphics;

namespace Content.Client.StationEvents;

public sealed partial class SolarFlareVisualsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private SolarFlareOverlay? _overlay = null;
    private readonly List<NetEntity> _solarFlares = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SolarFlareStartedEvent>(OnStarted);
        SubscribeNetworkEvent<SolarFlareEndedEvent>(OnEnded);
    }

    private void OnStarted(SolarFlareStartedEvent args)
    {
        if (_solarFlares.Any())
        {
            _solarFlares.Add(args.Gamerule);
            return;
        }

        _overlay = new();
        _overlayManager.AddOverlay(_overlay);
        _solarFlares.Add(args.Gamerule);
    }

    private void OnEnded(SolarFlareEndedEvent args)
    {
        _solarFlares.Remove(args.Gamerule);

        if (_overlay == null)
            return;

        if (_solarFlares.Any())
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
