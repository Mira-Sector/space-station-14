using Content.Shared.Modules.Components;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules;

public partial class ModuleSystem
{
    private void InitializeRequirements()
    {
        SubscribeLocalEvent<ModuleExclusivityComponent, ModuleAddingAttemptContainerEvent>(OnExclusivityAdd);
    }

    private void OnExclusivityAdd(Entity<ModuleExclusivityComponent> ent, ref ModuleAddingAttemptContainerEvent args)
    {
        if (args.Cancelled)
            return;

        var matches = 0;

        foreach (var module in GetModules(args.Container))
        {
            if (!_whitelist.CheckBoth(module, ent.Comp.Blacklist, ent.Comp.Whitelist))
                matches++;
        }

        if (matches >= ent.Comp.Maximum || matches < ent.Comp.Minimum)
        {
            args.Reason = ent.Comp.Popup;
            args.Cancel();
        }
    }
}
