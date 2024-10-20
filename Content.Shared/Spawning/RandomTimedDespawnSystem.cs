using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Shared.Spawning;

public sealed class RandomTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomTimedDespawnComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, RandomTimedDespawnComponent component, ref ComponentInit args)
    {
        var despawnComp = EnsureComp<TimedDespawnComponent>(uid);
        despawnComp.Lifetime = _random.NextFloat(component.Min, component.Max);
    }
}
