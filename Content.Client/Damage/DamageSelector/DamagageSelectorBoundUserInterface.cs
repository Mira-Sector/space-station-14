using Content.Client.UserInterface.Controls;
using Content.Shared.Body.Part;
using Content.Shared.Damage.DamageSelector;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.Damage.DamageSelector;

[UsedImplicitly]
public sealed class DamageSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SpriteSystem _sprite;

    private SimpleRadialMenu? _menu;

    public DamageSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entManager.System<SpriteSystem>();
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

        DamagePartSelectorEntry[] entries;

        if (GetMainButton(damageSelector, out var mainIndex))
        {
            var mainEntry = damageSelector.SelectableParts[mainIndex.Value];
            _menu.ContextualButton.TextureNormal = _sprite.Frame0(mainEntry.Sprite);
            _menu.ContextualButton.BackgroundColor = RadialMenuTextureButtonWithSector.DefaultBackgroundColor;
            _menu.ContextualButton.HoverBackgroundColor = RadialMenuTextureButtonWithSector.DefaultHoverBackgroundColor;
            _menu.CenterButtonAction = _ => HandleCenterRadialMenuClick(mainEntry.BodyPart);
            _menu.CloseButtonStyleClass = null;

            entries = new DamagePartSelectorEntry[damageSelector.SelectableParts.Length - 1];

            if (mainIndex > 0)
                Array.Copy(damageSelector.SelectableParts, 0, entries, 0, mainIndex.Value);

            if (mainIndex < damageSelector.SelectableParts.Length - 1)
                Array.Copy(damageSelector.SelectableParts, mainIndex.Value + 1, entries, mainIndex.Value, damageSelector.SelectableParts.Length - mainIndex.Value - 1);
        }
        else
        {
            _menu = new SimpleRadialMenu();
            entries = damageSelector.SelectableParts;
        }

        var otherButtons = ConvertToButtons(entries);
        _menu.SetButtons(otherButtons);
        _menu.OpenOverMouseScreenPosition();
    }

    private static bool GetMainButton(DamagePartSelectorComponent damageSelector, [NotNullWhen(true)] out int? index)
    {
        for (index = 0; index < damageSelector.SelectableParts.Length; index++)
        {
            var entry = damageSelector.SelectableParts[index.Value];
            if (entry.BodyPart.Type != damageSelector.MainPart.Type)
                continue;

            if (entry.BodyPart.Side != damageSelector.MainPart.Side)
                continue;

            return true;
        }

        index = null;
        return false;
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

    private void HandleCenterRadialMenuClick(BodyPart part)
    {
        HandleRadialMenuClick(part);
        Close();
    }

    private void HandleRadialMenuClick(BodyPart part)
    {
        var ev = new DamageSelectorSystemMessage(part);
        SendMessage(ev);
    }
}
