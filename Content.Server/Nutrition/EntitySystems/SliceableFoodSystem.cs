using Content.Server.DoAfter;
using Content.Server.Botany.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Kitchen.Components;
using Content.Server.Stack;
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

public sealed class SliceableFoodSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableFoodComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SliceableFoodComponent, SliceFoodDoAfterEvent>(OnSlicedoAfter);
        SubscribeLocalEvent<SliceableFoodComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnInteractUsing(Entity<SliceableFoodComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<UtensilComponent>(args.Used, out var utensil) || (utensil.Types & UtensilType.Knife) == 0) //if used item isn't a knife untensil, deny.
        {
            if (entity.Comp.AnySharp == false || entity.Comp.AnySharp == true && !HasComp<SharpComponent>(args.Used)) //alternatively, if any sharp item is allowed and doesn't have a sharpcomponent, deny.
                return;
        }

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

    private void OnSlicedoAfter(Entity<SliceableFoodComponent> entity, ref SliceFoodDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (TrySliceFood(entity, args.User, entity.Comp))
            args.Handled = true;
    }

    private bool TrySliceFood(EntityUid uid,
        EntityUid user,
        SliceableFoodComponent? component = null,
        FoodComponent? food = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref component, ref food, ref transform) ||
            string.IsNullOrEmpty(component.Slice) && string.IsNullOrEmpty(component.SliceStack))
            return false;

        if (!_solutionContainer.TryGetSolution(uid, food.Solution, out var soln, out var solution))
            return false;

        var isExtraSolution = false;
        Entity<SolutionComponent>? solnEx = null; //define here, replace with real values if needed.
        Solution? solutionEx = solution; //on the one hand, this implementation isn't great. On the other hand, when are you going to need three solution containers transferred?
        string? extraSolution = null;
        if (!string.IsNullOrEmpty(component.ExtraSolution))//check to see if extra solution is defined. If it isn't it should be ignored.
        {
            if (!_solutionContainer.TryGetSolution(uid, component.ExtraSolution, out var solnExtra, out var solutionExtra))
            {
                return false;
            }
            isExtraSolution = true;
            solnEx = solnExtra.Value; //if it was defined, apply the values actually wanted.
            solutionEx = solutionExtra;
            extraSolution = component.ExtraSolution;

        }

        var sliceVolume = solution.Volume / FixedPoint2.New(component.TotalCount);
        var sliceVolumeExtra = solutionEx.Volume / FixedPoint2.New(component.TotalCount);
        var sliceNumber = component.TotalCount;

        if (component.PotencyEffectsCount == true) //will potency effect the number of slices
        {
            if (TryComp<ProduceComponent>(uid, out var prod)) //if so, is there a produce component?
            {
                if (prod.Seed != null) //Is seed data defined? Wouldn't be for spawned produce.
                    sliceNumber = (ushort)Math.Ceiling(sliceNumber * prod.Seed.Potency / 100); //divide by potency as a percentage, round up to nearest whole number
            }
        }
        if (!string.IsNullOrEmpty(component.Slice))
        {
            for (int i = 0; i < sliceNumber; i++) //if value given for Slice, spawn slices
            {
                var sliceUid = Slice(uid, user, component, transform);
                if (component.TransferReagents != false)
                {
                    var lostSolution =
                        _solutionContainer.SplitSolution(soln.Value, sliceVolume);

                    // Fill new slice
                    FillSlice(sliceUid, lostSolution);

                    if (isExtraSolution == true && solnEx != null) //if there is an extra solution, add that one too
                    {
                        FillSliceExtra(sliceUid, solnEx.Value, sliceVolumeExtra, extraSolution);
                    }
                }
            }
        }
        else //otherwise, spawn single stack
        {
            SliceStack(uid, user, sliceNumber, component, transform);
            //Transferring reagents to stacks only does it for the first item in the stack. The rest are generic prototypes.
            //Probably a way to do this properly but stacks just seem to be stored as a number of generic prototypes, so not sure if good idea.
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
        SliceableFoodComponent? comp = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref transform))
            return EntityUid.Invalid;

        var sliceUid = Spawn(comp.Slice, _transform.GetMapCoordinates(uid));

        // try putting the slice into the container if the food being sliced is in a container!
        // this lets you do things like slice a pizza up inside of a hot food cart without making a food-everywhere mess
        _transform.DropNextTo(sliceUid, (uid, transform));
        _transform.SetLocalRotation(sliceUid, Angle.Zero);

        if (!_container.IsEntityOrParentInContainer(sliceUid))
        {
            var randVect = _random.NextVector2(comp.SpawnOffset, comp.SpawnOffset);
            if (TryComp<PhysicsComponent>(sliceUid, out var physics))
                _physics.SetLinearVelocity(sliceUid, randVect, body: physics);
        }

        return sliceUid;
    }

    public EntityUid SliceStack(EntityUid uid, EntityUid user, int count, SliceableFoodComponent? comp = null, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref comp, ref transform))
            return EntityUid.Invalid;

        if (comp.SliceStack == null)
            return EntityUid.Invalid;

        var sliceUid = _stack.Spawn(count, comp.SliceStack.Value, Transform(uid).Coordinates);

        _transform.DropNextTo(sliceUid, (uid, transform));
        _transform.SetLocalRotation(sliceUid, Angle.Zero);

        if (!_container.IsEntityOrParentInContainer(sliceUid))
        {
            var randVect = _random.NextVector2(comp.SpawnOffset, comp.SpawnOffset);
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
            _transform.SetLocalRotation(trashUid, Angle.Zero);
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

    private void FillSliceExtra(EntityUid sliceUid, Entity<SolutionComponent> solnValue, FixedPoint2 sliceExtra, string? extra) //fill up an extra solution, such as drink.
    {
        var solution = _solutionContainer.SplitSolution(solnValue, sliceExtra);
        if (_solutionContainer.TryGetSolution(sliceUid, extra, out var itsSoln, out var itsSolution))
        {
            _solutionContainer.RemoveAllSolution(itsSoln.Value);

            var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
            _solutionContainer.TryAddSolution(itsSoln.Value, lostSolutionPart);
        }
    }

    private void OnComponentStartup(Entity<SliceableFoodComponent> entity, ref ComponentStartup args)
    {
        var foodComp = EnsureComp<FoodComponent>(entity);
        _solutionContainer.EnsureSolution(entity.Owner, foodComp.Solution, out _);
    }
}

