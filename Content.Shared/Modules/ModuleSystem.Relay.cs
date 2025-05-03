using Content.Shared.Clothing;
using Content.Shared.Modules.Components;
using Content.Shared.Modules.Events;

namespace Content.Shared.Modules;

public partial class ModuleSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<ModuleContainerComponent, ClothingGotEquippedEvent>(RelayToModules);
        SubscribeLocalEvent<ModuleContainerComponent, ClothingGotUnequippedEvent>(RelayToModules);
    }

    public void RelayToModules<T>(Entity<ModuleContainerComponent> ent, T args) where T : class
    {
        var ev = new ModuleRelayedEvent<T>(args, ent.Owner);
        RaiseEventToModules((ent.Owner, ent.Comp), ev);
    }

    public void RelayToModules<T>(Entity<ModuleContainerComponent> ent, ref T args) where T : struct
    {
        var ev = new ModuleRelayedEvent<T>(args, ent.Owner);
        RaiseEventToModules((ent.Owner, ent.Comp), ev);
    }
}
