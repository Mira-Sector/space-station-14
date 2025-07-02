using Content.Client.UserInterface.Controls;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client.Damage.DamageSelector;

[UsedImplicitly]
public sealed class DamageSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SimpleRadialMenu? _menu;

    public DamageSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!_entManager.TryGetComponent<DamagePartSelectorComponent>(Owner, out var damageSelector))
        {
            Close();
            return;
        }

        _menu = this.CreateWindow<SimpleRadialMenu>();

        var otherButtons = ConvertToButtons(damageSelector.SelectableParts);
        _menu.SetButtons(otherButtons);
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(DamagePartSelectorEntry[] entries)
    {
        foreach (var entry in entries)
            yield return ConvertToButton(entry);
    }

    private RadialMenuActionOption<BodyPart> ConvertToButton(DamagePartSelectorEntry entry)
    {
        return new RadialMenuActionOption<BodyPart>(HandleRadialMenuClick, entry.BodyPart)
        {
            Sprite = entry.Sprite,
        };
    }

    private void HandleRadialMenuClick(BodyPart part)
    {
        var ev = new DamageSelectorSystemMessage(part);
        SendMessage(ev);
    }
}
