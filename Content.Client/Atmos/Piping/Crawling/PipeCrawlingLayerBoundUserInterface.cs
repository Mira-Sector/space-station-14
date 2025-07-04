using Content.Client.UserInterface.Controls;
using Content.Shared.Atmos.Piping.Crawling.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.Piping.Crawling;

public sealed class PipeCrawlingLayerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedAtmosPipeLayersSystem _pipeLayers;

    private SimpleRadialMenu? _menu;

    public PipeCrawlingLayerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _pipeLayers = _entManager.System<SharedAtmosPipeLayersSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null || state is not PipeCrawlingLayerBoundUserInterfaceState cast)
            return;

        var buttonModels = ConvertToButtons(cast.Layers);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(Dictionary<AtmosPipeLayer, SpriteSpecifier> layers)
    {
        foreach (var (layer, sprite) in layers)
        {
            yield return new RadialMenuActionOption<AtmosPipeLayer>(HandleRadialMenuClick, layer)
            {
                Sprite = sprite,
                ToolTip = _pipeLayers.GetPipeLayerName(layer)
            };
        }
    }

    private void HandleRadialMenuClick(AtmosPipeLayer layer)
    {
        var ev = new PipeCrawlingLayerRadialMessage(layer);
        SendPredictedMessage(ev);
    }
}
