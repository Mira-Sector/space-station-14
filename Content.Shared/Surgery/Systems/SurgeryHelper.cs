using Content.Shared.Body.Part;

namespace Content.Shared.Surgery.Systems;

public static class SurgeryHelper
{
    public static string GetBodyPartLoc(BodyPart part)
    {
        return part switch
        {
            _ => "surgery-body-part-invalid"
        };
    }
}
