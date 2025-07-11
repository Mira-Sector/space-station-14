using System.Diagnostics.CodeAnalysis;
using Content.Server.BaseAnalyzer;
using Content.Server.Body.Components;
using Content.Server.Medical.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Medical;

public sealed class HealthAnalyzerSystem : BaseAnalyzerSystem<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    /// <inheritdoc/>
    public override void UpdateScannedUser(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(healthAnalyzer, HealthAnalyzerUiKey.Key))
            return;

        if (!HasComp<DamageableComponent>(target) && _bodySystem.GetBodyDamage(target) == null)
            return;

        var bodyTemperature = float.NaN;

        if (TryComp<TemperatureComponent>(target, out var temp))
            bodyTemperature = temp.CurrentTemperature;

        var bloodAmount = float.NaN;
        var bleeding = false;
        var unrevivable = false;

        if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(target, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out var bloodSolution))
        {
            bloodAmount = bloodSolution.FillFraction;
            bleeding = bloodstream.BleedAmount > 0;
        }

        if (TryComp<UnrevivableComponent>(target, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        _uiSystem.ServerSendUiMessage(healthAnalyzer.Owner, HealthAnalyzerUiKey.Key, new HealthAnalyzerScannedUserMessage(
            GetNetEntity(target),
            healthAnalyzer.Comp.AnalyzerType,
            bodyTemperature,
            bloodAmount,
            scanMode,
            bleeding,
            unrevivable
        ));
    }

    protected override Enum GetUiKey()
    {
        return HealthAnalyzerUiKey.Key;
    }

    protected override bool ScanTargetPopupMessage(Entity<HealthAnalyzerComponent> uid, AfterInteractEvent args, [NotNullWhen(true)] out string? message)
    {
        message = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        return true;
    }

    protected override bool ValidScanTarget(EntityUid? target)
    {
        return HasComp<MobStateComponent>(target);
    }
}
