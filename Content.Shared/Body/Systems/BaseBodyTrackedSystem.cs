using Content.Shared.Body.Events;

namespace Content.Shared.Body.Systems;

public abstract partial class BaseBodyTrackedSystem : EntitySystem
{
    protected void SubscribeTrackerAdded<TTracker, TTracked>(EntityEventRefHandler<TTracker, BodyTrackerAdded> handler)
        where TTracker : Component
        where TTracked : Component, new()
    {
        SubscribeLocalEvent<TTracker, BodyTrackerAdded>((u, c, a) => OnTrackerAdded<TTracker, TTracked>(u, c, ref a, handler));
    }

    private void OnTrackerAdded<TTracker, TTracked>(EntityUid uid, TTracker component, ref BodyTrackerAdded args, EntityEventRefHandler<TTracker, BodyTrackerAdded> handler)
        where TTracker : Component
        where TTracked : Component, new()
    {
        if (args.ComponentName != Factory.GetComponentName<TTracked>())
            return;

        handler.Invoke((uid, component), ref args);
    }

    protected void SubscribeTrackerRemoved<TTracker, TTracked>(EntityEventRefHandler<TTracker, BodyTrackerRemoved> handler)
        where TTracker : Component
        where TTracked : Component, new()
    {
        SubscribeLocalEvent<TTracker, BodyTrackerRemoved>((u, c, a) => OnTrackerRemoved<TTracker, TTracked>(u, c, ref a, handler));
    }

    private void OnTrackerRemoved<TTracker, TTracked>(EntityUid uid, TTracker component, ref BodyTrackerRemoved args, EntityEventRefHandler<TTracker, BodyTrackerRemoved> handler)
        where TTracker : Component
        where TTracked : Component, new()
    {
        if (args.ComponentName != Factory.GetComponentName<TTracked>())
            return;

        handler.Invoke((uid, component), ref args);
    }
}
