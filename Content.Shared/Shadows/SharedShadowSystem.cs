using Content.Shared.Shadows.Components;
using Content.Shared.Physics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using JetBrains.Annotations;
using System.Numerics;
using System.Linq;

namespace Content.Shared.Shadows;

public abstract partial class SharedShadowSystem : ComponentTreeSystem<ShadowTreeComponent, ShadowCasterComponent>
{
    [Dependency] private readonly OccluderSystem _occluder = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;

    private const float MinRecalculateDistance = 0.8f;
    private const float MinRecalculateDistanceSquared = MinRecalculateDistance * MinRecalculateDistance;

    protected EntityQuery<ShadowCasterComponent> CasterQuery;

    protected override bool DoFrameUpdate { get; } = true;

    protected override bool DoTickUpdate { get; } = true;

    protected override bool Recursive { get; } = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowCasterComponent, ComponentInit>(OnCasterInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadowCasterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.NextRecalculation > _timing.CurTime)
                continue;

            comp.NextRecalculation += comp.RecalculateDelay;
            Dirty(uid, comp);

            if (xform.GridUid is not { } grid)
                continue;

            if ((xform.LocalPosition - comp.LastRecalculationPos).LengthSquared() < MinRecalculateDistanceSquared)
            {
                if (!TryUpdateCasterShadowOcclusion((uid, comp)))
                    continue;
            }
            else
            {
                comp.PreviousOccluders = []; // we have moved so this is incorrect
                if (!TryUpdateCasterShadowOcclusion((uid, comp)))
                    continue;
            }

            comp.LastRecalculationPos = xform.LocalPosition;

            var treeComp = Comp<ShadowTreeComponent>(grid);
            GenerateGridShadow((grid, treeComp));
            Dirty(uid, comp);
        }
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<ShadowCasterComponent> entry, Vector2 pos, Angle rot)
    {
        var vecRadius = new Vector2(entry.Component.Radius);
        return new(pos - vecRadius, pos + vecRadius);
    }

    private void OnCasterInit(Entity<ShadowCasterComponent> ent, ref ComponentInit args)
    {
        GenerateCasterShadow(ent);
        TryUpdateCasterShadowOcclusion(ent);
    }

    private void GenerateCasterShadow(Entity<ShadowCasterComponent> ent)
    {
        ent.Comp.PreviousOccluders = [];

        if (ent.Comp.Radius <= 0 || ent.Comp.Intensity < ShadowData.MinIntensity)
        {
            ent.Comp.UnoccludedShadowMap = [];
            Dirty(ent);
            return;
        }

        var diameter = ent.Comp.Radius * 2;
        var totalCells = diameter * diameter;
        Dictionary<Vector2i, ShadowData> shadowMap = new(totalCells);

        for (var x = -ent.Comp.Radius; x <= ent.Comp.Radius; x++)
        {
            for (var y = -ent.Comp.Radius; y <= ent.Comp.Radius; y++)
            {
                var dist = MathF.Sqrt(x * x + y * y);
                if (dist > ent.Comp.Radius)
                    continue;

                var pos = new Vector2i(x, y);
                var direction = new Vector2(x, y);
                var angleFromVertical = MathF.Acos(Math.Clamp(direction.Y, -1f, 1f));
                var falloff = MathF.Max(0f, 1f - angleFromVertical / ShadowData.MaxAngle);

                var attenuation = 1f - dist / ent.Comp.Radius;

                var strength = Math.Clamp(falloff * attenuation * ent.Comp.Intensity, 0f, 1f);
                shadowMap[pos] = new ShadowData(direction, strength);
            }
        }

        ent.Comp.UnoccludedShadowMap = shadowMap;
        Dirty(ent);
    }

    private bool TryUpdateCasterShadowOcclusion(Entity<ShadowCasterComponent> ent)
    {
        var casterXform = Transform(ent.Owner);
        var casterPos = Xform.GetWorldPosition(casterXform);

        var vecRadius = new Vector2(ent.Comp.Radius);
        var aabb = new Box2(casterPos - vecRadius, casterPos + vecRadius);
        var occludersEnt = _occluder.QueryAabb(casterXform.MapID, aabb);

        // fast pass
        if (!occludersEnt.Any())
        {
            Dirty(ent);
            return false;
        }

        HashSet<EntityUid> occluders = new(occludersEnt.Count);
        HashSet<Vector2i> occludersPos = new(occludersEnt.Count);
        foreach (var occluder in occludersEnt)
        {
            occluders.Add(occluder.Uid);
            var occluderXform = occluder.Transform;
            occludersPos.Add((Vector2i)occluderXform.LocalPosition);
        }

        // same occlusion as last time
        if (ent.Comp.PreviousOccluders.SetEquals(occludersPos))
            return false;

        ent.Comp.ShadowMap = ent.Comp.UnoccludedShadowMap;
        ent.Comp.PreviousOccluders = occludersPos;

        var angleStep = MathF.Tau / ent.Comp.Radius;
        for (var i = -ent.Comp.Radius; i <= ent.Comp.Radius; i++)
        {
            var angle = new Angle(angleStep * i);
            var dir = angle.ToVec();
            var ray = new CollisionRay(casterPos, dir, (int)CollisionGroup.Opaque);
            var results = _physics.IntersectRayWithPredicate(casterXform.MapID, ray, ent.Comp.Radius, e => occluders.Contains(e), true);
            if (!results.Any())
                continue;

            var hit = results.First(); // return on first hit is true so only need to check one
            var shadowMapPos = (Vector2i)(hit.HitPos - casterPos);

            var blockDir = dir * -1;

            var remainingDistance = ent.Comp.Radius - hit.Distance;
            var currentPos = hit.HitPos;
            while (remainingDistance > 0f)
            {
                var newShadowMapPos = (Vector2i)(currentPos - casterPos);

                var existing = ent.Comp.ShadowMap[newShadowMapPos];
                ent.Comp.ShadowMap[newShadowMapPos] = new ShadowData(existing.Direction, 0f);

                currentPos += blockDir;
                remainingDistance -= 1f;
            }
        }

        Dirty(ent);
        return true;
    }

    private void GenerateGridShadow(Entity<ShadowTreeComponent> ent)
    {
        var query = ent.Comp.Tree.GetEnumerator();
        while (query.MoveNext())
        {
            var current = query.Current;
            var casterUid = current.Uid;
            var casterComp = current.Component;
            var xform = current.Transform;

            var pos = (Vector2i)Vector2.Round(xform.LocalPosition);
            var basePos = pos + casterComp.Offset;

            foreach (var (localOffset, shadow) in casterComp.ShadowMap)
            {
                var worldPos = basePos + localOffset;
                var chunk = GetOrCreateChunk(ent, worldPos);

                // combine if a shadow already exists here
                if (chunk.ShadowMap.TryGetValue(worldPos, out var existing))
                    chunk.ShadowMap[worldPos] = ShadowData.Combine(existing, shadow);
                else
                    chunk.ShadowMap[worldPos] = shadow;
            }
        }

        Dirty(ent);
    }

    [PublicAPI]
    public void SetRadius(Entity<ShadowCasterComponent?> ent, int radius)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Radius == radius)
            return;

        ent.Comp.Radius = radius;
        Dirty(ent);
    }

    [PublicAPI]
    public void SetIntensity(Entity<ShadowCasterComponent?> ent, float intensity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Intensity == intensity)
            return;

        ent.Comp.Intensity = intensity;
        Dirty(ent);
    }

    [PublicAPI]
    public void SetOffset(Entity<ShadowCasterComponent?> ent, Vector2i offset)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Offset == offset)
            return;

        ent.Comp.Offset = offset;
        Dirty(ent);
    }

    [PublicAPI]
    public ShadowChunk GetOrCreateChunk(Entity<ShadowTreeComponent> ent, Vector2i tilePos)
    {
        var chunkPos = new Vector2i(tilePos.X / ShadowTreeComponent.ChunkSize, tilePos.Y / ShadowTreeComponent.ChunkSize);
        if (!ent.Comp.Chunks.TryGetValue(chunkPos, out var chunk))
        {
            chunk = new ShadowChunk(chunkPos);
            ent.Comp.Chunks[chunkPos] = chunk;
            Dirty(ent);
        }
        return chunk;
    }
}
