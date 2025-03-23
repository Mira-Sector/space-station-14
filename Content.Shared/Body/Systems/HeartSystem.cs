using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mobs.Systems;
using System.Linq;

namespace Content.Shared.Body.Systems;

public sealed class HeartSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, DefibrillateAttemptEvent>(OnBodyDefib);

        SubscribeLocalEvent<HeartComponent, StartedRottingEvent>(OnHeartRotting);
        SubscribeLocalEvent<HeartComponent, RotUpdateEvent>(OnHeartRotUpdate);
        SubscribeLocalEvent<HeartComponent, ComponentInit>(OnHeartInit);

        SubscribeLocalEvent<HeartComponent, UseInHandEvent>(OnHeartUseInHand);
        SubscribeLocalEvent<HeartComponent, BodyOrganRelayedEvent<DefibrillateAttemptEvent>>(OnHeartDefib);
        SubscribeLocalEvent<HeartComponent, ExaminedEvent>(OnHeartExamined);
    }

    private void OnHeartUseInHand(EntityUid uid, HeartComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_rotting.IsRotten(uid))
            return;

        component.Beating ^= true;
        _appearance.SetData(uid, HeartVisuals.Beating, component.Beating);
        args.Handled = true;
    }

    private void OnBodyDefib(EntityUid uid, BodyComponent component, DefibrillateAttemptEvent args)
    {
        if (!_body.GetBodyOrganEntityComps<HeartComponent>((uid, component)).Any())
        {
            args.Cancel();
            args.Reason = "defibrillator-heart-missing";
            return;
        }

        _body.RelayToOrgans(uid, component, args);
    }

    private void OnHeartDefib(EntityUid uid, HeartComponent component, ref BodyOrganRelayedEvent<DefibrillateAttemptEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        if (component.Beating)
            return;

        args.Args.Cancel();
        args.Args.Reason = "defibrillator-heart-off";
    }

    private void OnHeartRotting(EntityUid uid, HeartComponent component, StartedRottingEvent args)
    {
        _appearance.SetData(uid, HeartVisuals.Beating, false);
        component.Beating = false;
    }

    private void OnHeartRotUpdate(EntityUid uid, HeartComponent component, RotUpdateEvent args)
    {
        component.CurrentRespirationMultiplier = component.HealthyRespirationMultiplier + args.RotProgress * (component.DamagedRespirationMultiplier - component.HealthyRespirationMultiplier);
    }

    private void OnHeartInit(EntityUid uid, HeartComponent component, ComponentInit args)
    {
        component.CurrentRespirationMultiplier = component.HealthyRespirationMultiplier;
    }

    private void OnHeartExamined(EntityUid uid, HeartComponent component, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(component.Beating ? component.BeatingExamine : component.NotBeatingExamine));
    }
}
