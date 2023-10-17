using System.Collections.ObjectModel;

namespace PhysEdJournal.Api.Endpoints.Common;

public enum EndpointType
{
    Query,
    Command,
}

public static class EndpointTypeExtensions
{
    private static ReadOnlyDictionary<EndpointType, string> TypeToString { get; } =
        new Dictionary<EndpointType, string>
        {
            { EndpointType.Query, "Query" },
            { EndpointType.Command, "Command" },
        }.AsReadOnly();

    public static string Stringify(this EndpointType type)
    {
        return TypeToString[type];
    }
}
