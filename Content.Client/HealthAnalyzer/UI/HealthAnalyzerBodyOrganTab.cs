using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Systems;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerBodyOrganTab : BaseHealthAnalyzerBodyTab
{
    private readonly BodyDamageThresholdsSystem _damageThresholdsSystem;

    private (ProtoId<OrganPrototype> Organ, BodyDamageableComponent Damageable)? _selectedOrgan;

    public HealthAnalyzerBodyOrganTab(HealthAnalyzerBodyWindow window, IEntityManager entityManager, SharedBodySystem bodySystem, IPrototypeManager prototypeManager, SpriteSystem spriteSystem) : base(window, entityManager, bodySystem, prototypeManager, spriteSystem)
    {
        _damageThresholdsSystem = entityManager.System<BodyDamageThresholdsSystem>();
        Name = Loc.GetString("health-analyzer-window-tab-organ");
    }

    protected override bool TryGetTotalDamage(Entity<HealthAnalyzerBodyComponent> target, [NotNullWhen(true)] out FixedPoint2? totalDamage)
    {
        totalDamage = null;
        var organs = BodySystem.GetBodyOrganEntityComps<BodyDamageableComponent>(target.Owner);
        if (!organs.Any())
            return false;

        totalDamage = FixedPoint2.Zero;
        foreach (var organ in organs)
            totalDamage += organ.Comp1.Damage;

        return true;
    }

    protected override IEnumerable<HealthAnalyzerBodyButton> GetButtons(Entity<HealthAnalyzerBodyComponent> target)
    {
        foreach (var organ in BodySystem.GetBodyOrganEntityComps<BodyDamageableComponent>(target.Owner))
        {
            if (!EntityManager.TryGetComponent<HealthAnalyzerBodyItemComponent>(organ.Owner, out var data))
                continue;

            if (!_damageThresholdsSystem.TryGetThreshold(organ.Owner, BodyDamageState.Dead, out var deadThreshold))
                continue;

            var progressBar = GetProgressBar(data.ProgressBarLocation);
            var barLabelSuffix = $"{organ.Comp2.OrganType.ToString().ToLower()}";
            UpdateProgressBar(progressBar, organ.Comp2.OrganType, organ.Owner, barLabelSuffix, (float)organ.Comp1.Damage, (float)deadThreshold);
            yield return new HealthAnalyzerBodyButton(organ.Comp2.OrganType, organ.Owner, data.HoverSprite, data.SelectedSprite, SpriteSystem);
        }
    }

    protected override void ButtonPressed(IHealthAnalyzerBodyButton button)
    {
        if (button.Identifier is not ProtoId<OrganPrototype> organ || button.Owner == null)
            return;

        _selectedOrgan = (organ, EntityManager.GetComponent<BodyDamageableComponent>(button.Owner.Value));
    }

    protected override void ActOnButton()
    {
        if (_selectedOrgan == null)
            return;

        var (organ, _) = _selectedOrgan.Value;

        foreach (var child in LimbButton.Children)
        {
            if (child is not HealthAnalyzerBodyButton button)
                continue;

            if (button.Identifier is not ProtoId<OrganPrototype> buttonOrgan)
                continue;

            if (buttonOrgan != organ)
                continue;

            button.Selected();
            break;
        }
        LimbDamageLabel.Text = Loc.GetString($"[font size=16]{Loc.GetString($"health-analyzer-body-{organ.ToString().ToLower()}")}[/font]");
    }

    protected override void DrawDamageSidebar()
    {
        GroupsContainer.RemoveAllChildren();

        if (_selectedOrgan == null)
            return;

        var text = Loc.GetString("health-analyzer-window-damage-organ-text", ("amount", (float)_selectedOrgan.Value.Damageable.Damage));
        var label = BaseHealthAnalyzerWindow.CreateDiagnosticItemLabel(text, 1.125f);
        GroupsContainer.AddChild(label);
    }

}
