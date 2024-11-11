using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Light;

public abstract class SharedLightColorCycleSystem : EntitySystem
{
    [Dependency] protected readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
}
