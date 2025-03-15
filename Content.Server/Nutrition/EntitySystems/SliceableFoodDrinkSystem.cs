using Content.Server.DoAfter;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class SliceableFoodDrinkSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableFoodDrinkComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SliceableFoodDrinkComponent, SliceFoodDoAfterEvent>(OnSlicedoAfter);
        SubscribeLocalEvent<SliceableFoodDrinkComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnInteractUsing(Entity<SliceableFoodDrinkComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            entity.Comp.SliceTime,
            new SliceFoodDoAfterEvent(),
            entity,
            entity,
            args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnSlicedoAfter(Entity<SliceableFoodDrinkComponent> entity, ref SliceFoodDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (TrySliceFood(entity, args.User, args.Used, entity.Comp))
            args.Handled = true;
    }

    private bool TrySliceFood(EntityUid uid,
        EntityUid user,
        EntityUid? usedItem,
        SliceableFoodDrinkComponent? component = null,
        FoodComponent? food = null,
        TransformComponent? transform = null)
    {

        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;
        //log.Debug($"chop");

        if (!Resolve(uid, ref component, ref food, ref transform) ||
            string.IsNullOrEmpty(component.Slice))
            return false;

        if (!_solutionContainer.TryGetSolution(uid, food.Solution, out var soln, out var solution))
            return false;

        if (!TryComp<UtensilComponent>(usedItem, out var utensil) || (utensil.Types & UtensilType.Knife) == 0)
            return false;

        if (!_solutionContainer.TryGetSolution(uid, component.Extra, out var solnDrink, out var solutionDrink))
            return false;

        //log.Debug($"has drink");

        var sliceVolume = solution.Volume / FixedPoint2.New(component.TotalCount);
        var sliceVolumeDrink = solutionDrink.Volume / FixedPoint2.New(component.TotalCount);

        for (int i = 0; i < component.TotalCount; i++)
        {
            var sliceUid = Slice(uid, user, component, transform);

            var lostSolution =
                _solutionContainer.SplitSolution(soln.Value, sliceVolume);

            // Fill new slice
            FillSlice(sliceUid, lostSolution);
            //log.Debug($"check drink");

            var lostSolutionDrink =
                _solutionContainer.SplitSolution(solnDrink.Value, sliceVolumeDrink);
            //var lostSolutionDrink =
            //    _solutionContainer.SplitSolution(soln.Value, sliceVolume);
            FillSliceDrink(sliceUid, lostSolutionDrink, component.Extra);

        }

        _audio.PlayPvs(component.Sound, transform.Coordinates, AudioParams.Default.WithVolume(-2));
        var ev = new SliceFoodEvent();
        RaiseLocalEvent(uid, ref ev);

        DeleteFood(uid, user, food);
        return true;
    }

    /// <summary>
    /// Create a new slice in the world and returns its entity.
    /// The solutions must be set afterwards.
    /// </summary>
    public EntityUid Slice(EntityUid uid,
        EntityUid user,
        SliceableFoodDrinkComponent? comp = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref transform))
            return EntityUid.Invalid;

        var sliceUid = Spawn(comp.Slice, _transform.GetMapCoordinates(uid));

        // try putting the slice into the container if the food being sliced is in a container!
        // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
        _transform.DropNextTo(sliceUid, (uid, transform));
        _transform.SetLocalRotation(sliceUid, 0);

        if (!_container.IsEntityOrParentInContainer(sliceUid))
        {
            var randVect = _random.NextVector2(2.0f, 2.5f);
            if (TryComp<PhysicsComponent>(sliceUid, out var physics))
                _physics.SetLinearVelocity(sliceUid, randVect, body: physics);
        }

        return sliceUid;
    }

    private void DeleteFood(EntityUid uid, EntityUid user, FoodComponent foodComp)
    {
        var ev = new BeforeFullySlicedEvent
        {
            User = user
        };
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        // Locate the sliced food and spawn its trash
        foreach (var trash in foodComp.Trash)
        {
            var trashUid = Spawn(trash, _transform.GetMapCoordinates(uid));

            // try putting the trash in the food's container too, to be consistent with slice spawning?
            _transform.DropNextTo(trashUid, uid);
            _transform.SetLocalRotation(trashUid, 0);
        }

        QueueDel(uid);
    }

    private void FillSlice(EntityUid sliceUid, Solution solution)
    {
        // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
        if (TryComp<FoodComponent>(sliceUid, out var sliceFoodComp) &&
            _solutionContainer.TryGetSolution(sliceUid, sliceFoodComp.Solution, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }

    private void FillSliceDrink(EntityUid sliceUid, Solution solution, String extra) //fill up an extra solution, defaults to drink.
    {
        if (_solutionContainer.TryGetSolution(sliceUid, extra, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }

    private void OnComponentStartup(Entity<SliceableFoodDrinkComponent> entity, ref ComponentStartup args)
    {
        var foodComp = EnsureComp<FoodComponent>(entity);
        _solutionContainer.EnsureSolution(entity.Owner, foodComp.Solution, out _);
    }
}

