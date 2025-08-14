using Content.Shared.Surgery.UI;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private SurgeryWindow? _window;

    private EntityUid? _target = null;

    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SurgeryWindow>();
        _window.UpdateState(_target);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (state is not SurgeryBoundUserInterfaceState surgeryState)
            return;

        _target = _entityManager.GetEntity(surgeryState.Target);
        _window?.UpdateState(_target);
    }
}
