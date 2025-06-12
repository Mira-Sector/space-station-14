using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Emoting;
using Content.Shared.Speech.Muting;

namespace Content.Shared.Body.Systems;

public sealed class TongueSystem : BaseBodyTrackedSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TongueComponent, OrganAddedEvent>(OnOrganAdded);

        SubscribeLocalEvent<TongueContainerComponent, EmoteAttemptEvent>(OnTongueContainerEmoteAttempt);

        SubscribeTrackerAdded<TongueContainerComponent, TongueComponent>(OnTrackerAdded);
        SubscribeTrackerRemoved<TongueContainerComponent, TongueComponent>(OnTrackerRemoved);
    }

    private void OnOrganAdded(Entity<TongueComponent> ent, ref OrganAddedEvent args)
    {
        if (!TryComp<BodyPartComponent>(args.Part, out var bodyPartComp))
            return;

        EnsureComp<TongueContainerComponent>(args.Part);
        _body.RegisterTracker<TongueComponent>(args.Part);

        if (bodyPartComp.Body is { } body)
        {
            EnsureComp<TongueContainerComponent>(body);
            _body.RegisterTracker<TongueComponent>(body);
        }
    }

    private void OnTongueContainerEmoteAttempt(Entity<TongueContainerComponent> ent, ref EmoteAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_body.GetTrackerCount<TongueComponent>(ent.Owner) > 0)
            return;

        args.Cancel();
    }

    private void OnTrackerAdded(Entity<TongueContainerComponent> ent, ref BodyTrackerAdded args)
    {
        if (!HasComp<MutedComponent>(ent))
            return;

        RemComp<MutedComponent>(ent);
    }

    private void OnTrackerRemoved(Entity<TongueContainerComponent> ent, ref BodyTrackerRemoved args)
    {
        if (args.Count > 0)
            return;

        EnsureComp<MutedComponent>(ent);
    }
}
