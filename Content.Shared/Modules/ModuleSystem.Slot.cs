using Content.Shared.Interaction;
using Content.Shared.Modules.Components;
using Content.Shared.Modules.Events;
using Content.Shared.Wires;

namespace Content.Shared.Modules;

public partial class ModuleSystem
{
    private void InitializeSlot()
    {
        SubscribeLocalEvent<ModuleContainerAddOnInteractComponent, InteractUsingEvent>(OnAddOnInteract);
        SubscribeLocalEvent<ModuleContainerRequireWirePanelComponent, ModuleContainerModuleAddingAttemptEvent>(OnWirePanelAttempt);
    }

    private void OnAddOnInteract(Entity<ModuleContainerAddOnInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ModuleContainerComponent>(ent.Owner, out var moduleContainer))
            return;

        args.Handled = _container.Insert(args.Used, moduleContainer.Modules);
    }

    private void OnWirePanelAttempt(Entity<ModuleContainerRequireWirePanelComponent> ent, ref ModuleContainerModuleAddingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var wiresPanel) && !wiresPanel.Open)
            args.Cancel();
    }
}
