using Content.Shared.Chunking;
using Content.Shared.Shadows;
using Content.Shared.Shadows.Components;
using Content.Shared.Shadows.Events;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

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

        SubscribeLocalEvent<ShadowGridComponent, ComponentGetState>(OnGridGetState);
    }

    private void OnGridGetState(Entity<ShadowGridComponent> ent, ref ComponentGetState args)
    {
        if (args.Player is not { } session)
        {
            // send full state
            args.State = new ShadowGridState(GetNetEntitySet(ent.Comp.Casters), ent.Comp.Chunks);
            return;
        }

        var netGrid = GetNetEntity(ent.Owner);

        var chunksInRange = _chunking.GetChunksForSession(session, ShadowGridComponent.ChunkSize, _chunkIndexPool, _chunkViewerPool);
        if (!chunksInRange.TryGetValue(netGrid, out var chunkIndexes))
        {
            // blank
            args.State = new ShadowGridState(GetNetEntitySet(ent.Comp.Casters), []);
            return;
        }

        try
        {
            Dictionary<Vector2i, ShadowChunk> toSend = new(chunkIndexes.Count);
            foreach (var chunkIndex in chunkIndexes)
            {
                if (!ent.Comp.Chunks.TryGetValue(chunkIndex, out var chunk))
                    continue;

                toSend[chunkIndex] = chunk;
            }

            args.State = new ShadowGridState(GetNetEntitySet(ent.Comp.Casters), toSend);
        }
        catch
        {
            args.State = new ShadowGridState(GetNetEntitySet(ent.Comp.Casters), []);
        }
        finally
        {
            foreach (var chunk in chunksInRange.Values)
                _chunkIndexPool.Return(chunk);

            _chunkViewerPool.Return(chunksInRange);
        }
    }

#if DEBUG
    public void ToggleDebugOverlay(ICommonSession session)
    {
        var ev = new ToggleShadowDebugOverlayEvent();
        RaiseNetworkEvent(ev, session);
    }
#endif
}
