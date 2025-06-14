using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public partial class SharedPipeCrawlingSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private void InitializeAction()
    {
        SubscribeLocalEvent<PipeCrawlingComponent, PipeCrawlingLayerActionEvent>(OnActionAction);
        SubscribeAllEvent<PipeCrawlingLayerRadialMessage>(OnActionRadial);
    }

    private void SetActionIcon(Entity<PipeCrawlingComponent> ent)
    {
        if (ent.Comp.LayerAction is not { } action)
            return;

        if (!TryComp<PipeCrawlingLayerActionComponent>(action, out var layerAction))
            return;

        if (!layerAction.IconSprites.TryGetValue(ent.Comp.CurrentLayer, out var icon))
            icon = SpriteSpecifier.Invalid;

        _actions.SetIcon(action, icon);
    }

    private void OnActionAction(Entity<PipeCrawlingComponent> ent, ref PipeCrawlingLayerActionEvent args)
    {
        if (!TryComp<ActorComponent>(args.Performer, out var actorComp))
            return;

        if (!TryComp<PipeCrawlingLayerActionComponent>(args.Action, out var layerAction))
            return;

        if (_ui.IsUiOpen(ent.Owner, PipeCrawlingLayerUiKey.Key, args.Performer))
        {
            _ui.CloseUi(ent.Owner, PipeCrawlingLayerUiKey.Key, actorComp.PlayerSession);
            return;
        }

        if (!TryComp<PipeCrawlingPipeComponent>(ent.Comp.CurrentPipe, out var pipe))
            return;

        Dictionary<AtmosPipeLayer, SpriteSpecifier> layers = [];

        foreach (var (layer, _) in pipe.ConnectedPipes)
        {
            if (!layerAction.IconSprites.TryGetValue(layer, out var icon))
                icon = SpriteSpecifier.Invalid;

            layers.Add(layer, icon);
        }

        if (layers.Count <= 1)
            return;

        var state = new PipeCrawlingLayerBoundUserInterfaceState(layers);

        _ui.OpenUi(ent.Owner, PipeCrawlingLayerUiKey.Key, actorComp.PlayerSession);
        _ui.SetUiState(ent.Owner, PipeCrawlingLayerUiKey.Key, state);
    }

    private void OnActionRadial(PipeCrawlingLayerRadialMessage args)
    {
        var entity = GetEntity(args.Entity);
        if (!TryComp<PipeCrawlingComponent>(entity, out var crawling))
            return;

        crawling.CurrentLayer = args.Layer;
        Dirty(entity, crawling);

        SetActionIcon((entity, crawling));
    }
}
