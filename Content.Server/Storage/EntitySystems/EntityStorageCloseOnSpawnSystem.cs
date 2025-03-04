using Content.Server.Humanoid.Systems;
using Content.Server.Storage.Components;
using Robust.Shared.Timing;

namespace Content.Server.Storage.EntitySystems;

public sealed class EntityStorageCloseOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageCloseOnSpawnComponent, ComponentInit>(OnCloseOnSpawnInit, after: new[] { typeof(RandomHumanoidAppearanceSystem) });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EntityStorageCloseOnSpawnComponent>();
        while (query.MoveNext(out var uid, out var closeComp))
        {
            if (_timing.CurTime > closeComp.CloseAt)
                continue;

            RemCompDeferred(uid, closeComp);
            _entityStorage.CloseStorage(uid);
        }
    }

    private void OnCloseOnSpawnInit(EntityUid uid, EntityStorageCloseOnSpawnComponent component, ComponentInit args)
    {
        if (component.Delay == TimeSpan.Zero)
        {
            RemCompDeferred(uid, component);
            _entityStorage.CloseStorage(uid);
            return;
        }

        component.CloseAt = _timing.CurTime + component.Delay;
    }

}
