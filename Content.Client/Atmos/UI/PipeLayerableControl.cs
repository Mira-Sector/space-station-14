using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Client.Items.UI;
using Content.Shared.Atmos.Piping.Layerable;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Atmos.UI;

public sealed class PipeLayerableControl : PollingItemStatusControl<PipeLayerableControl.Data>
{
    private readonly PipeLayerableComponent _parent;
    private readonly RichTextLabel _label;

    public PipeLayerableControl(PipeLayerableComponent parent)
    {
        _parent = parent;
        _label = new RichTextLabel {StyleClasses = {StyleNano.StyleClassItemStatus}};
        _label.SetMarkup(Loc.GetString("pipe-layerable-status", ("layer", _parent.Layer)));
        AddChild(_label);

        UpdateDraw();
    }


    protected override Data PollData()
    {
        return new Data(_parent.Layer);
    }

    protected override void Update(in Data data)
    {
        _label.SetMarkup(Loc.GetString("pipe-layerable-status", ("layer", data.Layer)));
    }

    public readonly record struct Data(int Layer);
}

