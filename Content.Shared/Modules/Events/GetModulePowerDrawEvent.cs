namespace Content.Shared.Modules.Events;

public sealed partial class GetModulePowerDrawEvent : EntityEventArgs
{
    public float Additional = 0f;
    public float Multiplier = 1f;
}
