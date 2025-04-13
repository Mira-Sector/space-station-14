using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Speech.Muting;

namespace Content.Shared.Body.Systems;

public sealed class TongueSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TongueComponent, OrganAddedEvent>(OnOrganAdded);
        SubscribeLocalEvent<TongueComponent, OrganRemovedEvent>(OnOrganRemoved);

        SubscribeLocalEvent<TongueContainerComponent, BodyPartAddedEvent>(OnTongueContainerAdded);
        SubscribeLocalEvent<TongueContainerComponent, BodyPartRemovedEvent>(OnTongueContainerRemoved);
    }

    private void OnOrganAdded(Entity<TongueComponent> ent, ref OrganAddedEvent args)
    {
        EnsureComp<TongueContainerComponent>(args.Part, out var tongueContainerComp);
        tongueContainerComp.Tongues += 1;
        Dirty(args.Part, tongueContainerComp);

        if (!TryComp<BodyPartComponent>(args.Part, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<TongueContainerComponent>(body, out var bodyTongueComp);
        bodyTongueComp.Tongues += 1;
        Dirty(body, bodyTongueComp);

        CheckMute((body, bodyTongueComp));
    }

    private void OnOrganRemoved(Entity<TongueComponent> ent, ref OrganRemovedEvent args)
    {
        var tongueContainerComp = EntityManager.GetComponent<TongueContainerComponent>(args.OldPart);
        tongueContainerComp.Tongues -= 1;
        Dirty(args.OldPart, tongueContainerComp);

        if (!TryComp<BodyPartComponent>(args.OldPart, out var bodyPartComp) || bodyPartComp.Body is not {} body)
            return;

        EnsureComp<TongueContainerComponent>(body, out var bodyTongueComp);
        bodyTongueComp.Tongues -= 1;
        Dirty(body, bodyTongueComp);

        CheckMute((body, bodyTongueComp));
    }

    private void OnTongueContainerAdded(Entity<TongueContainerComponent> ent, ref BodyPartAddedEvent args)
    {
        if (!TryComp<TongueContainerComponent>(args.Part, out var partTongueComp))
            return;

        ent.Comp.Tongues += partTongueComp.Tongues;
        Dirty(ent);

        CheckMute(ent);
    }

    private void OnTongueContainerRemoved(Entity<TongueContainerComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<TongueContainerComponent>(args.Part, out var partTongueComp))
            return;

        ent.Comp.Tongues -= partTongueComp.Tongues;
        Dirty(ent);

        CheckMute(ent);
    }

    private void CheckMute(Entity<TongueContainerComponent> ent)
    {
        if (ent.Comp.Tongues > 0)
        {
            if (!HasComp<MutedComponent>(ent))
                return;

            RemComp<MutedComponent>(ent);
        }
        else
        {
            EnsureComp<MutedComponent>(ent);
        }
    }
}
