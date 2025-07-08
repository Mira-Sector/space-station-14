using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerBodyBodyTab : BaseHealthAnalyzerBodyTab
{
    private (BodyPart Part, DamageableComponent Damageable)? _selectedPart;

    public HealthAnalyzerBodyBodyTab(HealthAnalyzerBodyWindow window, IEntityManager entityManager, SharedBodySystem bodySystem, IPrototypeManager prototypeManager, SpriteSystem spriteSystem) : base(window, entityManager, bodySystem, prototypeManager, spriteSystem)
    {
        Name = Loc.GetString("health-analyzer-window-tab-body");
    }

    protected override bool TryGetTotalDamage(Entity<HealthAnalyzerBodyComponent> target, [NotNullWhen(true)] out FixedPoint2? totalDamage)
    {
        totalDamage = null;
        if (BodySystem.GetBodyDamage(target.Owner) is not { } damage)
            return false;

        totalDamage = damage.GetTotal();
        return true;
    }

    protected override void DrawButtons(Entity<HealthAnalyzerBodyComponent> target)
    {
        foreach (var (limbUid, limbComp) in BodySystem.GetBodyChildren(target.Owner))
        {
            var part = new BodyPart(limbComp.PartType, limbComp.Symmetry);
            if (!EntityManager.TryGetComponent<HealthAnalyzerBodyItemComponent>(limbUid, out var data))
                continue;

            if (!EntityManager.TryGetComponent<DamageableComponent>(limbUid, out var damageable))
                continue;

            if (!EntityManager.TryGetComponent<BodyPartThresholdsComponent>(limbUid, out var thresholdsComp) || !thresholdsComp.Thresholds.TryGetValue(WoundState.Dead, out var deadThreshold))
                continue;

            var progressBar = GetProgressBar(data.ProgressBarLocation);
            var barLabelSuffix = $"{part.Side.ToString().ToLower()}-{part.Type.ToString().ToLower()}";
            UpdateProgressBar(progressBar, barLabelSuffix, (float)damageable.TotalDamage, (float)deadThreshold);
            var button = new HealthAnalyzerBodyButton(part, limbUid, data.HoverSprite, data.SelectedSprite, SpriteSystem);
            LimbButton.AddChild(button);
        }
    }

    protected override void ButtonPressed(HealthAnalyzerBodyButton button)
    {
        if (button.Identifier is not BodyPart part)
            return;

        _selectedPart = (part, EntityManager.GetComponent<DamageableComponent>(button.Owner));
    }

    protected override void ActOnButton()
    {
        if (_selectedPart == null)
        {
            foreach (var child in LimbButton.Children)
            {
                if (child is HealthAnalyzerBodyButton button)
                    button.Selected();
            }

            LimbDamageLabel.Text = $"[font size=16]{Loc.GetString("health-analyzer-body-all")}[/font]";
        }
        else
        {
            var (part, _) = _selectedPart.Value;

            foreach (var child in LimbButton.Children)
            {
                if (child is not HealthAnalyzerBodyButton button)
                    continue;

                if (button.Identifier is not BodyPart buttonPart)
                    continue;

                if (buttonPart.Type != part.Type)
                    continue;

                if (buttonPart.Side != part.Side)
                    continue;

                button.Selected();
                break;
            }
            LimbDamageLabel.Text = Loc.GetString($"[font size=16]{Loc.GetString($"health-analyzer-body-{part.Side.ToString().ToLower()}-{part.Type.ToString().ToLower()}")}[/font]");
        }
    }

    protected override void DrawDamageSidebar()
    {
        GroupsContainer.RemoveAllChildren();

        if (_selectedPart == null)
            return;

        var damageSortedGroups =
            _selectedPart.Value.Damageable.Damage.GetDamagePerGroup(PrototypeManager).OrderByDescending(damage => damage.Value)
                .ToDictionary(x => x.Key, x => x.Value);

        foreach (var container in HealthAnalyzerWindow.DrawDiagnosticGroups(damageSortedGroups, _selectedPart.Value.Damageable.Damage.DamageDict, 1.125f))
            GroupsContainer.AddChild(container);
    }

}
