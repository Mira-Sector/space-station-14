using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.UI;
using Robust.Client.UserInterface;

namespace Content.Client.Surgery.UI;

public sealed partial class SurgeryBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private SurgeryWindow? _window;

    private Entity<SurgeryReceiverBodyComponent>? _target = null;

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

        if (state is not SurgeryBoundUserInterfaceState surgeryState)
            return;

        var targetUid = _entityManager.GetEntity(surgeryState.Target);
        if (!_entityManager.TryGetComponent<SurgeryReceiverBodyComponent>(targetUid, out var receiverBodyComponent))
            return;

        _target = (targetUid.Value, receiverBodyComponent);

        _window?.UpdateState(_target);
    }
}
