using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhysEdJournal.Api.Endpoints.Endpoint;

public sealed class CustomProblemDetailsJsonConverter : JsonConverter<ProblemDetailsResponse>
{
    private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");
    private static readonly JsonEncodedText Title = JsonEncodedText.Encode("title");
    private static readonly JsonEncodedText StatusCode = JsonEncodedText.Encode("status_code");
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

        writer.WriteNumber(StatusCode, value.StatusCode);

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

[JsonConverter(typeof(CustomProblemDetailsJsonConverter))]
public sealed class ProblemDetailsResponse
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("status_code")]
    public required int StatusCode { get; init; }

    [JsonPropertyName("detail")]
    public required string Detail { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; init; }
}
