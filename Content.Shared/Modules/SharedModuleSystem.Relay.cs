using Content.Shared.Clothing;
using Content.Shared.Modules.Components;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Events;

namespace Content.Shared.Modules;

public partial class SharedModuleSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<ModuleContainerComponent, ClothingGotEquippedEvent>(RelayToModules);
        SubscribeLocalEvent<ModuleContainerComponent, ClothingGotUnequippedEvent>(RelayToModules);

        SubscribeLocalEvent<ModuleContainerComponent, ModSuitContainerPartSealedEvent>(RelayToModules);
        SubscribeLocalEvent<ModuleContainerComponent, ModSuitContainerPartUnsealedEvent>(RelayToModules);
    }

    public void RelayToModules<T>(Entity<ModuleContainerComponent> ent, ref T args)
    {
        var ev = new ModuleRelayedEvent<T>(args, ent.Owner);
        RaiseEventToModules((ent.Owner, ent.Comp), ev);
    }
}
