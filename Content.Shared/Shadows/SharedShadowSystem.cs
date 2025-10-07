using Content.Shared.Shadows.Components;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Shadows;

public abstract partial class SharedShadowSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;

    private const float MinStrength = 0.001f;
    private const float MinRecalculateDistance = 0.1f;
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

            if (EnsureComp<ShadowGridComponent>(grid, out var gridComp) && gridComp.Casters.Contains(uid))
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
        if (ent.Comp.Radius < MinRecalculateDistance || ent.Comp.Intensity < MinStrength)
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
                var angle = new Vector2(x, y).Normalized();
                var strength = Math.Clamp(1f - dist / ent.Comp.Radius, 0f, 1f) * ent.Comp.Intensity;
                shadowMap[pos] = new ShadowData(angle, strength);
            }
        }

        // special case
        // prevent nans
        ent.Comp.ShadowMap[new(0, 0)] = new ShadowData(Vector2.Zero, ent.Comp.Intensity);

        ent.Comp.ShadowMap = shadowMap;
        Dirty(ent);
    }

    private void GenerateGridShadow(Entity<ShadowGridComponent> ent)
    {
        var estimatedCells = 0f;
        foreach (var casterUid in ent.Comp.Casters)
        {
            if (!CasterQuery.TryComp(casterUid, out var caster))
                continue;

            estimatedCells += MathF.PI * caster.Radius * caster.Radius;
        }
        ent.Comp.ShadowMap.EnsureCapacity((int)Math.Ceiling(estimatedCells));
        ent.Comp.ShadowMap.Clear();

        foreach (var caster in ent.Comp.Casters)
        {
            var casterComp = CasterQuery.Comp(caster);
            var pos = Transform(caster).LocalPosition;

            var basePos = new Vector2i((int)MathF.Round(pos.X + casterComp.Offset.X), (int)MathF.Round(pos.Y + casterComp.Offset.Y));

            foreach (var (localOffset, shadow) in casterComp.ShadowMap)
            {
                var worldPos = basePos + localOffset;

                // combine if a shadow already exists here
                if (ent.Comp.ShadowMap.TryGetValue(worldPos, out var existing))
                    ent.Comp.ShadowMap[worldPos] = CombineShadow(existing, shadow);
                else
                    ent.Comp.ShadowMap[worldPos] = shadow;
            }
        }

        Dirty(ent);
    }

    private static ShadowData CombineShadow(ShadowData a, ShadowData b)
    {
        var totalStrength = a.Strength + b.Strength;
        if (totalStrength < MinStrength)
            return new ShadowData(Vector2.Zero, 0f);

        // weighted direction average
        var weightedDir = (a.Direction * a.Strength + b.Direction * b.Strength) / totalStrength;
        var dirNorm = weightedDir.Normalized();

        var strength = MathF.Min(totalStrength, 1f);
        return new ShadowData(dirNorm, strength);
    }
}
