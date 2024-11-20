using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Surgery.Steps;

[TypeSerializer]
public sealed class SurgeryGraphStepTypeSerializer : ITypeReader<SurgeryGraphStep, MappingDataNode>
{
    private Type? GetType(MappingDataNode node)
    {
        if (node.Has("tool"))
            return typeof(ToolSurgeryGraphStep);

        return null;
    }

    public SurgeryGraphStep Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SurgeryGraphStep>? instanceProvider = null)
    {
        var type = GetType(node) ??
                    throw new ArgumentException(
                        "Tried to convert invalid YAML node mapping to SurgeryGraphStep!");

        return (SurgeryGraphStep)serializationManager.Read(type, node, hookCtx, context)!;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var type = GetType(node);

        if (type == null)
            return new ErrorNode(node, "No surgery graph step type found.");

        return serializationManager.ValidateNode(type, node, context);
    }
}
