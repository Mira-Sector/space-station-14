using Content.Shared.Camera.ShakeData;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using JetBrains.Annotations;
using System.Numerics;
using System.Linq;

namespace Content.Shared.Camera;

public sealed partial class CameraShakeSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CameraShakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        RemQueue<Entity<CameraShakeComponent, EyeComponent>> remQueue = new();

        var query = EntityQueryEnumerator<CameraShakeComponent, EyeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var eye))
        {
            var changed = false;
            List<CameraShakeEntry> newEntries = new(comp.Entries.Count);

            foreach (var entry in comp.Entries)
            {
                var newEntry = entry;
                newEntry.Elapsed += TimeSpan.FromSeconds(frameTime);

                if (newEntry.Elapsed < newEntry.Duration)
                {
                    newEntries.Add(newEntry);
                    continue;
                }

                // expired
                // we only dirty this when we need to remove an entry
                // this gets run every tick so...
                // and client is also doing this so no need to predict everything else
                changed = true;
                continue;
            }

            if (!newEntries.Any())
            {
                remQueue.Add((uid, comp, eye));
                continue;
            }

            comp.Entries = newEntries;
            _eye.UpdateEyeOffset((uid, eye));

            if (changed)
                Dirty(uid, comp);
        }

        foreach (var (uid, comp, eye) in remQueue)
        {
            RemComp(uid, comp);
            _eye.UpdateEyeOffset((uid, eye));
        }
    }

    private void OnGetEyeOffset(Entity<CameraShakeComponent> ent, ref GetEyeOffsetEvent args)
    {
        args.Offset += GetCombinedOffset(ent);
    }

    private Vector2 GetCombinedOffset(Entity<CameraShakeComponent> ent)
    {
        var vec = Vector2.Zero;
        foreach (var entry in ent.Comp.Entries)
            vec += GetEntryOffset(ent, entry);

        return vec;
    }

    private Vector2 GetEntryOffset(Entity<CameraShakeComponent> ent, CameraShakeEntry entry)
    {
        if (entry.Duration <= TimeSpan.Zero)
            return Vector2.Zero;

        if (!entry.DirectionData.TryGetDirection(ent, EntityManager, out var entryDir))
            return Vector2.Zero;

        var progress = (float)(entry.Elapsed.TotalSeconds / entry.Duration.TotalSeconds);
        progress = Math.Clamp(progress, 0f, 1f);

        var falloff = 1f - progress;
        falloff *= falloff;

        var magnitude = MathHelper.Lerp(entry.MaxMagnitude, entry.MinMagnitude, progress) * falloff;

        // randomnessn isnt predicted
        // we need to seed this with something we can share with client and server
        var t = (int)Math.Floor(entry.Elapsed.TotalSeconds * entry.Frequency) + 1; // +1 to prevent the same seed at 0 seconds
        var seed = entry.Seed ^ t;
        _random.SetSeed(seed);
        var noiseVec = _random.NextVector2();

        var dirWeight = 1 - entry.NoiseWeight;
        var weightedDir = entryDir * dirWeight;
        var weightedNoise = noiseVec * entry.NoiseWeight;
        var mixed = Vector2.Normalize(weightedDir + weightedNoise);
        return mixed * magnitude;
    }

    [PublicAPI]
    public void ShakeCameraDirection(EntityUid uid,
        Vector2 direction,
        float minMagnitude,
        float maxMagnitude,
        float noiseWeight,
        TimeSpan duration,
        float frequency)
    {
        var data = new CameraShakeDirectionData()
        {
            Direction = direction
        };

        ShakeCamera(uid, data, minMagnitude, maxMagnitude, noiseWeight, duration, frequency);
    }

    [PublicAPI]
    public void ShakeCameraEntity(EntityUid uid,
        EntityUid target,
        float minMagnitude,
        float maxMagnitude,
        float noiseWeight,
        TimeSpan duration,
        float frequency)
    {
        var data = new CameraShakeEntityData()
        {
            Target = target
        };

        ShakeCamera(uid, data, minMagnitude, maxMagnitude, noiseWeight, duration, frequency);
    }

    [PublicAPI]
    public void ShakeCameraPosition(EntityUid uid,
        EntityCoordinates coords,
        float minMagnitude,
        float maxMagnitude,
        float noiseWeight,
        TimeSpan duration,
        float frequency)
    {
        var data = new CameraShakePositionalData()
        {
            Position = coords
        };

        ShakeCamera(uid, data, minMagnitude, maxMagnitude, noiseWeight, duration, frequency);
    }

    private void ShakeCamera(EntityUid uid,
        ICameraShakeData data,
        float minMagnitude,
        float maxMagnitude,
        float noiseWeight,
        TimeSpan duration,
        float frequency)
    {
        var entry = new CameraShakeEntry()
        {
            DirectionData = data,
            MinMagnitude = minMagnitude,
            MaxMagnitude = maxMagnitude,
            NoiseWeight = noiseWeight,
            Duration = duration,
            Elapsed = TimeSpan.Zero,
            Frequency = frequency,
            Seed = _random.Next(),
        };
        EnsureComp<CameraShakeComponent>(uid, out var shake);
        shake.Entries.Add(entry);
        Dirty(uid, shake);
    }
}
