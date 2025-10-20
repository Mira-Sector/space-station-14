using Content.Shared.Shadows.Components;
using Robust.Shared.Timing;
using System.Numerics;
using JetBrains.Annotations;

namespace Content.Shared.Shadows;

public abstract partial class SharedShadowSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;

    private const float MinRecalculateDistance = 0.8f;
    private const float MinRecalculateDistanceSquared = MinRecalculateDistance * MinRecalculateDistance;

    protected EntityQuery<ShadowCasterComponent> CasterQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowCasterComponent, AfterAutoHandleStateEvent>(OnCasterHandleState);
        SubscribeLocalEvent<ShadowCasterComponent, ComponentInit>(OnCasterInit);
        SubscribeLocalEvent<ShadowCasterComponent, EntParentChangedMessage>(OnCasterParentChanged);

        SubscribeLocalEvent<ShadowGridComponent, ComponentInit>(OnGridInit);

        CasterQuery = GetEntityQuery<ShadowCasterComponent>();
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
                continue;

            comp.LastRecalculationPos = xform.LocalPosition;

            if (EnsureComp<ShadowGridComponent>(grid, out var gridComp) || gridComp.Casters.Contains(uid))
                GenerateGridShadow((grid, gridComp));
        }
    }

    private void OnCasterHandleState(Entity<ShadowCasterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Transform(ent.Owner).GridUid is not { } grid)
            return;

        EnsureComp<ShadowGridComponent>(grid);
    }

    private void OnCasterInit(Entity<ShadowCasterComponent> ent, ref ComponentInit args)
    {
        GenerateCasterShadow(ent);
    }

    private void OnCasterParentChanged(Entity<ShadowCasterComponent> ent, ref EntParentChangedMessage args)
    {
        if (TryComp<ShadowGridComponent>(args.OldParent, out var oldGrid))
        {
            if (oldGrid.Casters.Remove(ent.Owner))
                GenerateGridShadow((args.OldParent.Value, oldGrid));
        }

        if (TryComp<ShadowGridComponent>(args.Transform.GridUid, out var newGrid))
        {
            if (newGrid.Casters.Add(ent.Owner))
                GenerateGridShadow((args.Transform.GridUid.Value, newGrid));
        }
    }

    private void OnGridInit(Entity<ShadowGridComponent> ent, ref ComponentInit args)
    {
        var xform = Transform(ent.Owner);

        ent.Comp.Casters = new(xform.ChildCount);

        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!CasterQuery.HasComp(child))
                continue;

            ent.Comp.Casters.Add(child);
        }

        GenerateGridShadow(ent);
    }

    private void GenerateCasterShadow(Entity<ShadowCasterComponent> ent)
    {
        if (ent.Comp.Radius < MinRecalculateDistance || ent.Comp.Intensity < ShadowData.MinIntensity)
        {
            ent.Comp.ShadowMap = [];
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
                var direction = new Vector2(x, y).Normalized();
                var angleFromVertical = MathF.Acos(Math.Clamp(direction.Y, -1f, 1f));
                var falloff = MathF.Max(0f, 1f - angleFromVertical / ShadowData.MaxAngle);

                var attenuation = 1f - dist / ent.Comp.Radius;

                var strength = Math.Clamp(falloff * attenuation * ent.Comp.Intensity, 0f, 1f);
                shadowMap[pos] = new ShadowData(direction, strength);
            }
        }

        ent.Comp.ShadowMap = shadowMap;
        Dirty(ent);
    }

    private void GenerateGridShadow(Entity<ShadowGridComponent> ent)
    {
        List<Entity<ShadowCasterComponent>> casters = new(ent.Comp.Casters.Count);
        foreach (var casterUid in ent.Comp.Casters)
        {
            if (!Initialized(casterUid))
                continue;

            if (!CasterQuery.TryComp(casterUid, out var caster))
                continue;

            casters.Add((casterUid, caster));
        }

        foreach (var caster in casters)
        {
            var pos = (Vector2i)Vector2.Round(Transform(caster).LocalPosition);
            var basePos = pos + caster.Comp.Offset;

            foreach (var (localOffset, shadow) in caster.Comp.ShadowMap)
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
    public ShadowChunk GetOrCreateChunk(Entity<ShadowGridComponent> ent, Vector2i tilePos)
    {
        var chunkPos = new Vector2i(tilePos.X / ShadowGridComponent.ChunkSize, tilePos.Y / ShadowGridComponent.ChunkSize);
        if (!ent.Comp.Chunks.TryGetValue(chunkPos, out var chunk))
        {
            chunk = new ShadowChunk(chunkPos);
            ent.Comp.Chunks[chunkPos] = chunk;
        }
        return chunk;
    }
}
