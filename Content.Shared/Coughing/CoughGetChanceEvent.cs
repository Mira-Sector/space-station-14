namespace Content.Shared.Coughing;

public sealed partial class CoughGetChanceEvent : CancellableEntityEventArgs
{
    public float Chance;
}
