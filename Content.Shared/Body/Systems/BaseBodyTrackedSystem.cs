using Content.Shared.Body.Events;

namespace Content.Shared.Body.Systems;

public abstract partial class BaseBodyTrackedSystem : EntitySystem
{
    [Dependency] protected readonly SharedBodySystem Body = default!;

    private readonly Dictionary<string, Dictionary<string, Delegate>> _addSubscribers = [];
    private readonly Dictionary<string, Dictionary<string, Delegate>> _removeSubscribers = [];

    protected void SubscribeTrackerAdded<TTracker, TTracked>(EntityEventRefHandler<TTracker, BodyTrackerAdded> handler)
        where TTracker : Component, new()
        where TTracked : Component, new()
    {
        var trackerName = Factory.GetComponentName<TTracker>();
        var trackedName = Factory.GetComponentName<TTracked>();

        if (_addSubscribers.TryGetValue(trackerName, out var trackers))
        {
            trackers.Add(trackedName, handler);
        }
        else
        {
            trackers = [];
            trackers.Add(trackedName, handler);
            _addSubscribers.Add(trackerName, trackers);
            SubscribeLocalEvent<TTracker, BodyTrackerAdded>(OnTrackerAdded);
        }
    }

    private void OnTrackerAdded<TTracker>(EntityUid uid, TTracker component, ref BodyTrackerAdded args)
        where TTracker : Component, new()
    {
        var trackerName = Factory.GetComponentName<TTracker>();
        if (!_addSubscribers.TryGetValue(trackerName, out var trackers))
            return;

        foreach (var (tracked, handler) in trackers)
        {
            if (tracked != args.ComponentName)
                continue;

            if (handler is EntityEventRefHandler<TTracker, BodyTrackerAdded> castedHandler)
            {
                castedHandler.Invoke((uid, component), ref args);
                return;
            }
        }
    }

    protected void SubscribeTrackerRemoved<TTracker, TTracked>(EntityEventRefHandler<TTracker, BodyTrackerRemoved> handler)
        where TTracker : Component, new()
        where TTracked : Component, new()
    {
        var trackerName = Factory.GetComponentName<TTracker>();
        var trackedName = Factory.GetComponentName<TTracked>();

        if (_removeSubscribers.TryGetValue(trackerName, out var trackers))
        {
            trackers.Add(trackedName, handler);
        }
        else
        {
            trackers = [];
            trackers.Add(trackedName, handler);
            _removeSubscribers.Add(trackerName, trackers);
            SubscribeLocalEvent<TTracker, BodyTrackerRemoved>(OnTrackerRemoved);
        }
    }

    private void OnTrackerRemoved<TTracker>(EntityUid uid, TTracker component, ref BodyTrackerRemoved args)
        where TTracker : Component, new()
    {
        var trackerName = Factory.GetComponentName<TTracker>();
        if (!_removeSubscribers.TryGetValue(trackerName, out var trackers))
            return;

        foreach (var (tracked, handler) in trackers)
        {
            if (tracked != args.ComponentName)
                continue;

            if (handler is EntityEventRefHandler<TTracker, BodyTrackerRemoved> castedHandler)
            {
                castedHandler.Invoke((uid, component), ref args);
                return;
            }
        }
    }
}
