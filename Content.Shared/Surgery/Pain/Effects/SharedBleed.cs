namespace Content.Shared.Surgery.Pain.Effects;

public abstract partial class SharedBleed : SurgeryPainEffect
{
    [DataField(required: true)]
    public float Amount;
}
