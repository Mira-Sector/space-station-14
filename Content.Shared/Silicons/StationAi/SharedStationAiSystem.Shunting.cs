using Content.Shared.DoAfter;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.StationAi;


public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafterSystem = default!;

    private const string ShuntingContainer = "shunting";

    private void InitializeShunting()
    {
        SubscribeLocalEvent<StationAiShuntingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiShuntingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StationAiShuntingComponent, StationAiShuntingEvent>(OnShunt);
    }

    private void OnInit(EntityUid uid, StationAiShuntingComponent component, ComponentInit args)
    {
        _containers.EnsureContainer<ContainerSlot>(uid,ShuntingContainer);
    }

    private void OnAttempt(EntityUid uid, StationAiShuntingComponent component, StationAiShuntingAttemptEvent args)
    {
        if (!HasComp<StationAiCanShuntComponent>(args.User))
            return;

        _doafterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.Delay, new StationAiShuntingEvent(), uid, uid, progressBarOverride: uid));
    }

    private void OnShunt(EntityUid uid, StationAiShuntingComponent component, StationAiShuntingEvent args)
    {
        if (!_containers.TryGetContainer(uid, ShuntingContainer, out var container))
            return;

        if (_containers.TryGetContainingContainer(args.User, out var startingContainer))
        {
            // TODO: Re entry
            _containers.RemoveEntity(startingContainer.Owner, args.User);
        }

        _containers.Insert(args.User, container);
    }
}
