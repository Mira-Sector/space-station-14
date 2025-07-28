using Content.Shared.Body.Part;

namespace Content.Shared.Surgery.Systems;

public static class SurgeryHelper
{
    public static LocId GetBodyPartLoc(BodyPart part)
    {
        return part.Type switch
        {
            BodyPartType.Head => part.Side switch
            {
                BodyPartSymmetry.None => "surgery-body-part-head-none",
                _ => "surgery-body-part-invalid"
            },
            BodyPartType.Torso => part.Side switch
            {
                BodyPartSymmetry.None => "surgery-body-part-torso-none",
                _ => "surgery-body-part-invalid"
            },
            BodyPartType.Arm => part.Side switch
            {
                BodyPartSymmetry.Left => "surgery-body-part-arm-left",
                BodyPartSymmetry.Right => "surgery-body-part-arm-right",
                _ => "surgery-body-part-invalid"
            },
            BodyPartType.Leg => part.Side switch
            {
                BodyPartSymmetry.Left => "surgery-body-part-leg-left",
                BodyPartSymmetry.Right => "surgery-body-part-leg-right",
                _ => "surgery-body-part-invalid"
            },
            _ => "surgery-body-part-invalid"
        };
    }

    public static string BodyPartIconState(BodyPart part)
    {
        return part.Type switch
        {
            BodyPartType.Head => part.Side switch
            {
                BodyPartSymmetry.None => "head",
                _ => throw new NotImplementedException()
            },
            BodyPartType.Torso => part.Side switch
            {
                BodyPartSymmetry.None => "torso",
                _ => throw new NotImplementedException()
            },
            BodyPartType.Arm => part.Side switch
            {
                BodyPartSymmetry.Left => "larm",
                BodyPartSymmetry.Right => "rarm",
                _ => throw new NotImplementedException()
            },
            BodyPartType.Leg => part.Side switch
            {
                BodyPartSymmetry.Left => "lleg",
                BodyPartSymmetry.Right => "rleg",
                _ => throw new NotImplementedException()
            },
            _ => throw new NotImplementedException(),
        };
    }
}
