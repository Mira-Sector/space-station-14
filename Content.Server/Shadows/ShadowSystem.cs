using Content.Shared.Chunking;
using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Content.Shared.Shadows.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Microsoft.Extensions.ObjectPool;

namespace Content.Server.Shadows;

public sealed partial class ShadowSystem : SharedShadowSystem
{
    [Dependency] private readonly ChunkingSystem _chunking = default!;

    private readonly ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
        new DefaultObjectPool<HashSet<Vector2i>>(
            new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);

    private readonly ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> _chunkViewerPool =
        new DefaultObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>>(
            new DefaultPooledObjectPolicy<Dictionary<NetEntity, HashSet<Vector2i>>>(), 64);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowTreeComponent, ComponentGetState>(OnGridGetState);
    }

    private void OnGridGetState(Entity<ShadowTreeComponent> ent, ref ComponentGetState args)
    {
        if (args.Player is not { } session)
        {
            // send full state
            args.State = new ShadowTreeState(ent.Comp.Chunks);
            return;
        }

        var netGrid = GetNetEntity(ent.Owner);

        var chunksInRange = _chunking.GetChunksForSession(session, ShadowTreeComponent.ChunkSize, _chunkIndexPool, _chunkViewerPool);
        if (!chunksInRange.TryGetValue(netGrid, out var chunkIndexes))
            return;

        try
        {
            Dictionary<Vector2i, ShadowChunk> toSend = new(chunkIndexes.Count);
            foreach (var chunkIndex in chunkIndexes)
            {
                if (!ent.Comp.Chunks.TryGetValue(chunkIndex, out var chunk))
                    continue;

                toSend[chunkIndex] = chunk;
            }

            args.State = new ShadowTreeState(toSend);
        }
        finally
        {
            foreach (var set in chunksInRange.Values)
            {
                set.Clear();
                _chunkIndexPool.Return(set);
            }

            chunksInRange.Clear();
            _chunkViewerPool.Return(chunksInRange);
        }
    }

#if DEBUG
    public void ToggleDebugOverlay(ICommonSession session, bool showCasters)
    {
        var ev = new ToggleShadowDebugOverlayEvent(showCasters);
        RaiseNetworkEvent(ev, session);
    }
#endif
}
