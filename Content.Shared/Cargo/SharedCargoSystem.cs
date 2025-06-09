using System.Diagnostics.CodeAnalysis;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Cargo;

public abstract class SharedCargoSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationBankAccountComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StationBankAccountComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIncomeTime = Timing.CurTime + ent.Comp.IncomeDelay;
        Dirty(ent);
    }

    /// <summary>
    /// For a given station, retrieves the balance in a specific account.
    /// </summary>
    public int GetBalanceFromAccount(Entity<StationBankAccountComponent?> station, ProtoId<CargoAccountPrototype> account)
    {
        if (!Resolve(station, ref station.Comp))
            return 0;

        return station.Comp.Accounts.GetValueOrDefault(account);
    }

    /// <summary>
    /// For a station, creates a distribution between one the bank's account and the other accounts.
    /// The primary account receives the majority percentage listed on the bank account, with the remaining
    /// funds distributed to all accounts based on <see cref="StationBankAccountComponent.RevenueDistribution"/>
    /// </summary>
    public Dictionary<ProtoId<CargoAccountPrototype>, double> CreateAccountDistribution(Entity<StationBankAccountComponent> stationBank)
    {
        var distribution = new Dictionary<ProtoId<CargoAccountPrototype>, double>
        {
            { stationBank.Comp.PrimaryAccount, stationBank.Comp.PrimaryCut }
        };
        var remaining = 1.0 - stationBank.Comp.PrimaryCut;

        foreach (var (account, percentage) in stationBank.Comp.RevenueDistribution)
        {
            var existing = distribution.GetOrNew(account);
            distribution[account] = existing + remaining * percentage;
        }
        return distribution;
    }

    public static bool TryGetProductName(CargoProductPrototype product, [NotNullWhen(true)] out string? name)
    {
        name = product.Name ?? product.Data.GetName();
        return name != null;
    }

    public static bool TryGetProductDescription(CargoProductPrototype product, [NotNullWhen(true)] out string? description)
    {
        description = product.Description ?? product.Data.GetDescription();
        return description != null;
    }
}

[NetSerializable, Serializable]
public enum CargoConsoleUiKey : byte
{
    Orders,
    Bounty,
    Shuttle,
    Telepad
}

[NetSerializable, Serializable]
public enum CargoPalletConsoleUiKey : byte
{
    Sale
}
