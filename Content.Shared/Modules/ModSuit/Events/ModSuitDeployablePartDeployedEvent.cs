namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployablePartDeployedEvent(EntityUid suit, EntityUid? wearer, string slot, int partNumber) : BaseModSuitDeployablePartDeployEvent(suit, wearer, slot, partNumber);
