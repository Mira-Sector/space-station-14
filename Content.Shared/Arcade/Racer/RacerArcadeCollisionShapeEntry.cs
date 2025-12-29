using Content.Shared.Arcade.Racer.CollisionShapes;
using Content.Shared.Arcade.Racer.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Text;

namespace Content.Shared.Arcade.Racer;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RacerArcadeCollisionShapeEntry
{
    [DataField(customTypeSerializer: typeof(FlagSerializer<RacerArcadeCollisionFlags>))]
    public int Layer = (int)RacerArcadeCollisionGroups.All;

    [DataField(customTypeSerializer: typeof(FlagSerializer<RacerArcadeCollisionFlags>))]
    public int Mask = (int)RacerArcadeCollisionGroups.All;

    [DataField(required: true)]
    public BaseRacerArcadeObjectCollisionShape Shape;

    [DataField]
    public bool Hard = true;

    [ViewVariables]
    [Access(typeof(RacerArcadeObjectCollisionSystem), Other = AccessPermissions.None)]
    public Dictionary<NetEntity, Dictionary<string, NetRacerArcadeCollisionContact>> ObjectShapesCollided = [];

    [ViewVariables]
    [Access(typeof(RacerArcadeObjectCollisionSystem), Other = AccessPermissions.None)]
    public Dictionary<string, NetRacerArcadeCollisionContact> TrackShapesCollided = [];

    public override string ToString()
    {
        return $"Layer: {FormatFlags(Layer)} Mask: {FormatFlags(Mask)} Shape: ( {Shape} )";

        string FormatFlags(int flags)
        {
            var builder = new StringBuilder();

            var knownBits = 0;

            var values = GetValues();
            builder.AppendJoin('|', values);

            var unknown = flags & ~knownBits;
            if (unknown != 0)
            {
                builder.Append('|');
                builder.Append($"Unknown Bits({unknown:b})");
            }

            return builder.ToString();

            IEnumerable<RacerArcadeCollisionGroups> GetValues()
            {
                var groups = Enum.GetValues<RacerArcadeCollisionGroups>();

                foreach (var value in groups)
                {
                    var intValue = (int)value;
                    if ((flags & intValue) == 0)
                        continue;

                    knownBits |= intValue;
                    yield return value;
                }
            }
        }
    }
}
