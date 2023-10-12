using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhysEdJournal.Api.Endpoints.BaseEndpoint;

public sealed class CustomProblemDetailsJsonConverter : JsonConverter<ProblemDetailsResponse>
{
    private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");
    private static readonly JsonEncodedText Title = JsonEncodedText.Encode("title");
    private static readonly JsonEncodedText Status = JsonEncodedText.Encode("status");
    private static readonly JsonEncodedText Detail = JsonEncodedText.Encode("detail");

    public override ProblemDetailsResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException(); // Этот конвертер только для ответа сервера, поэтому нет смысла реализовывать чтение данных
    }

    public override void Write(
        Utf8JsonWriter writer,
        ProblemDetailsResponse value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        WriteProblemDetails(writer, value, options);
        writer.WriteEndObject();
    }

    private static void WriteProblemDetails(
        Utf8JsonWriter writer,
        ProblemDetailsResponse value,
        JsonSerializerOptions options
    )
    {
        writer.WriteString(Type, value.Type);

        writer.WriteString(Title, value.Title);

        writer.WriteNumber(Status, value.Status);

        writer.WriteString(Detail, value.Detail);

        if (value.Extensions is not null)
        {
            foreach (var kvp in value.Extensions)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(
                    writer,
                    kvp.Value,
                    kvp.Value?.GetType() ?? typeof(object),
                    options
                );
            }
        }
    }
}

[Newtonsoft.Json.JsonConverter(typeof(CustomProblemDetailsJsonConverter))]
public sealed class ProblemDetailsResponse
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("status")]
    public required int Status { get; init; }

    [JsonPropertyName("detail")]
    public required string Detail { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; init; }
}
