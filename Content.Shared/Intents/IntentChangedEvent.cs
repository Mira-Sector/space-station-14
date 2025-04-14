namespace Content.Shared.Intents;

public sealed partial class IntentChangedEvent : EntityEventArgs
{
    public Intent SelectedIntent;

    public Intent OldIntent;

    public IntentChangedEvent(Intent selectedIntent, Intent oldIntent)
    {
        SelectedIntent = selectedIntent;
        OldIntent = oldIntent;
    }
}
