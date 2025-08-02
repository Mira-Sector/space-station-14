namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitUnsealedEvent(EntityUid? wearer) : BaseModSuitSealEvent(wearer);
