using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Item.ItemToggle;

public sealed class ItemToggleExamineTextSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleExamineTextComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ItemToggleExamineTextComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<ItemToggleComponent>(ent.Owner, out var itemToggle))
            return;

        if (itemToggle.Activated)
        {
            if (ent.Comp.EnabledText != null)
                args.PushMarkup(Loc.GetString(ent.Comp.EnabledText));
        }
        else
        {
            if (ent.Comp.DisabledText != null)
                args.PushMarkup(Loc.GetString(ent.Comp.DisabledText));
        }
    }
}
