using Content.Shared.Maths;
using Content.Shared.PolygonRenderer;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

[Serializable, NetSerializable]
public sealed partial class Box : BaseRacerArcadeObjectCollisionShape
{
    [DataField("box")]
    public Box3 Box3 = Box3.UnitCentered;

    [DataField("rotation")]
    public Vector3 EulerRotation
    {
        get => Quaternion.ToEulerRad(Rotation);
        set
        {
            var qx = Quaternion.FromAxisAngle(Vector3.UnitX, value.X);
            var qy = Quaternion.FromAxisAngle(Vector3.UnitY, value.Y);
            var qz = Quaternion.FromAxisAngle(Vector3.UnitZ, value.Z);

            // roll * pitch * yaw
            Rotation = qy * qx * qz;
        }
    }

    [ViewVariables]
    public Quaternion Rotation;

    [DataField]
    public Vector3 Center;

    private static readonly ProtoId<PolygonModelPrototype> DebugModelId = "RacerCollisionBox";

    public override RacerArcadeObjectCollisionShapeComplexity Complexity => RacerArcadeObjectCollisionShapeComplexity.Box;

    public override Box3Rotated GetBox()
    {
        return new(Box3, Rotation, Center + Origin);
    }

    public override PolygonModel GetDebugModel(IPrototypeManager prototype)
    {
        var model = prototype.Index(DebugModelId);
        var scaleMatrix = Matrix4.Scale(Box3.Size);
        var rotMatrix = Matrix4.Rotate(Rotation);
        var offsetMatrix = Matrix4.CreateTranslation(Origin) * Matrix4.CreateTranslation(Center);
        model.ModelMatrix = scaleMatrix * rotMatrix * offsetMatrix;
        return model;
    }
}
