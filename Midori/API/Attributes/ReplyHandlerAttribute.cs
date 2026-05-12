namespace Midori.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ReplyHandlerAttribute : Attribute
{
    public Type CustomType { get; }

    public ReplyHandlerAttribute(Type customType)
    {
        CustomType = customType;
    }
}
