using Content.Client.Modules.ModSuit.Events;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Modules.ModSuit.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Modules.ModSuit;

public partial class ModSuitSystem
{
    private void InitializeDeployableRelay()
    {
        SubscribeLocalEvent<ModSuitDeployedPartComponent, AppearanceChangeEvent>(RelayToSuit);
        SubscribeLocalEvent<ModSuitDeployedPartComponent, GetEquipmentVisualsEvent>(RelayToSuit);
        SubscribeLocalEvent<ModSuitDeployedPartComponent, GetInhandVisualsEvent>(RelayToSuit);

        SubscribeLocalEvent<ModSuitDeployedPartComponent, ModSuitSealedGetIconLayersEvent>(RelayToSuit);
        SubscribeLocalEvent<ModSuitDeployedPartComponent, ModSuitSealedGetClothingLayersEvent>(RelayToSuit);
    }
}
