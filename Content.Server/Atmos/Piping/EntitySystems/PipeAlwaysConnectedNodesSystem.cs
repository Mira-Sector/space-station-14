using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed partial class PipeAlwaysConnectedNodesSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeAlwaysConnectedNodesComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<PipeAlwaysConnectedNodesComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        foreach (var node1Id in ent.Comp.Nodes)
        {
            if (!_nodeContainer.TryGetNode<PipeNode>(nodeContainer, node1Id, out var node1))
                continue;

            foreach (var node2Id in ent.Comp.Nodes)
            {
                if (node1Id == node2Id)
                    continue;

                if (!_nodeContainer.TryGetNode<PipeNode>(nodeContainer, node2Id, out var node2))
                    continue;

                node1.AddAlwaysReachable(node2);
            }
        }
    }
}
