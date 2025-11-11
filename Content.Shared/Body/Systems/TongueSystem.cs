using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Emoting;
using Content.Shared.Speech.Muting;

namespace Content.Shared.Body.Systems;

public sealed class TongueSystem : BaseBodyTrackedSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TongueComponent, OrganInitEvent>(OnOrganInit);

        SubscribeLocalEvent<TongueContainerComponent, EmoteAttemptEvent>(OnTongueContainerEmoteAttempt);

        SubscribeTrackerAdded<TongueContainerComponent, TongueComponent>(OnTrackerAdded);
        SubscribeTrackerRemoved<TongueContainerComponent, TongueComponent>(OnTrackerRemoved);
    }

    private void OnOrganInit(Entity<TongueComponent> ent, ref OrganInitEvent args)
    {
        EnsureComp<TongueContainerComponent>(args.Part);
        Body.RegisterTracker<TongueComponent>(args.Part.Owner);

        EnsureComp<TongueContainerComponent>(args.Body);
        Body.RegisterTracker<TongueComponent>(args.Body.Owner);
    }

    private void OnTongueContainerEmoteAttempt(Entity<TongueContainerComponent> ent, ref EmoteAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (Body.GetTrackerCount<TongueComponent>(ent.Owner) > 0)
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
