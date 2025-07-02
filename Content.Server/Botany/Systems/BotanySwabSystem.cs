using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Botany.Systems;

public sealed class BotanySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MutationSystem _mutationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BotanySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BotanySwabComponent, BotanySwabDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BotanySwabComponent, UseInHandEvent>(OnClean);
        SubscribeLocalEvent<BotanySwabComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BotanySwabComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    /// <summary>
    /// This handles swab examination text
    /// so you can tell if they are used or not.
    /// </summary>
    private void OnExamined(EntityUid uid, BotanySwabComponent swab, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (swab.SeedData != null)
                args.PushMarkup(Loc.GetString("swab-used"));
            else if (swab.Usable == true)
                args.PushMarkup(Loc.GetString("swab-unused"));
        }
    }

    /// <summary>
    /// Handles swabbing a plant.
    /// </summary>
    private void OnAfterInteract(EntityUid uid, BotanySwabComponent swab, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantHolderComponent>(args.Target))
            return;

        if (swab.Usable == false && swab.SeedData == null)
        {
            _popupSystem.PopupClient(Loc.GetString("botany-swab-unusable"), uid, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.SwabDelay, new BotanySwabDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// Save seed data or cross-pollenate.
    /// </summary>
    private void OnDoAfter(EntityUid uid, BotanySwabComponent swab, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !TryComp<PlantHolderComponent>(args.Args.Target, out var plant))
            return;

        _audioSystem.PlayPvs(swab.SwabSound, uid);
        if (swab.SeedData == null)
        {
            // Pick up pollen
            swab.SeedData = plant.Seed;
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-from"), args.Args.Target.Value, args.Args.User);
        }
        else
        {
            var old = plant.Seed;
            if (old == null)
                return;
            plant.Seed = _mutationSystem.Cross(swab.SeedData, old); // Cross-pollenate
            if (swab.Contaminate == true)
                swab.SeedData = old; // Transfer old plant pollen to swab
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Args.Target.Value, args.Args.User);
        }
        args.Handled = true;
    }

    private void OnClean(EntityUid uid, BotanySwabComponent swab, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (swab.Cleanable == true)
        {
            swab.SeedData = null;
            _popupSystem.PopupClient(Loc.GetString("botany-swab-clean"), uid, args.User);
            _audioSystem.PlayPvs(swab.CleanSound, uid);
        }
        args.Handled = true;
    }

    private void OnInsertAttempt(EntityUid uid, BotanySwabComponent swab, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (!TryComp<BotanySwabComponent>(args.Container.Owner, out var applicator)) //does the container have the botanySwab component
        {
            args.Cancel();
            return;
        }

        if (applicator.SeedData == null)
        {
            _popupSystem.PopupClient(Loc.GetString("swab-applicator-needs-pollen"), uid);
            args.Cancel();
            return;
        }

        applicator.SeedData = swab.SeedData;
    }

    private void OnRemoveAttempt(EntityUid uid, BotanySwabComponent swab, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (!TryComp<BotanySwabComponent>(args.Container.Owner, out var applicator)) //does the container have the botanySwab component
            return;

        applicator.SeedData = null;
    }
}
