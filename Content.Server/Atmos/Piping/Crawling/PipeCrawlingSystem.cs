using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping.Crawling.Components;

namespace Content.Server.Atmos.Piping.Crawling.Systems;

public sealed class PipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    const string NodeName = "pipe";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<PipeCrawlingComponent, ExhaleLocationEvent>(OnExhale);

        SubscribeLocalEvent<PipeCrawlingComponent, AtmosExposedGetAirEvent>(OnExposed);

    }

    private void OnInhale(EntityUid uid, PipeCrawlingComponent component, ref InhaleLocationEvent args)
    {
        if (_internals.AreInternalsWorking(uid))
            return;

        if (!_nodeContainer.TryGetNode(component.CurrentPipe, NodeName, out PipeNode? outlet))
            return;

        args.Gas = outlet.Air;
    }

    private void OnExhale(EntityUid uid, PipeCrawlingComponent component, ref ExhaleLocationEvent args)
    {
        if (_internals.AreInternalsWorking(uid))
            return;

        if (!_nodeContainer.TryGetNode(component.CurrentPipe, NodeName, out PipeNode? outlet))
            return;

        args.Gas = outlet.Air;
    }

    private void OnExposed(EntityUid uid, PipeCrawlingComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (!_nodeContainer.TryGetNode(component.CurrentPipe, NodeName, out PipeNode? outlet))
            return;

        args.Gas = outlet.Air;
        args.Handled = true;
    }
}
