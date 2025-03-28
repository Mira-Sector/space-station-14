using Content.Server.Botany.Components;
using Content.Server.Kitchen.Components;
using Content.Server.Stack;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Server.Botany.Systems;

public sealed class LogStackSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LogStackComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, LogStackComponent component, InteractUsingEvent args)
    {
        if (!HasComp<SharpComponent>(args.Used))
            return;


        var amount = component.SpawnCount; //also acts as fallback value, will be used if no seeddata to draw from in ProduceComponent, such as for spawned in produce.

        if (component.UsePotency)
        {
            if (!TryComp<ProduceComponent>(uid, out var prod))
                return;

            if (prod.Seed != null)
                amount = prod.Seed.Potency; //set to potency value if it exists
            amount = (float)Math.Ceiling((float)amount / component.PotencyDivisor); //round up to avoid zeros
        }

        // if in some container, try pick up, else just drop to world
        var inContainer = _containerSystem.IsEntityInContainer(uid);
        var pos = Transform(uid).Coordinates;

        var stackPrototype = _protoMan.Index<StackPrototype>(component.SpawnedPrototype);
        var spawned = _stack.Spawn((int)amount, component.SpawnedPrototype, pos);

        if (inContainer)
            _handsSystem.PickupOrDrop(args.User, spawned);
        else
        {
            var xform = Transform(spawned);
            _containerSystem.AttachParentToContainerOrGrid((spawned, xform));
            xform.LocalRotation = 0;
            _randomHelper.RandomOffset(spawned, component.RandomOffset);
        }

        QueueDel(uid);
    }
}
