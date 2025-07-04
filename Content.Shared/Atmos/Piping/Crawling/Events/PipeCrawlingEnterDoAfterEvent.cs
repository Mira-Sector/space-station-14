using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Crawling.Events;

[Serializable, NetSerializable]
public sealed partial class PipeCrawlingEnterDoAfterEvent : SimpleDoAfterEvent;
