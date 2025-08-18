using System.Text.Json;
using System.Text.Json.Serialization;
using Octokit;

namespace GitHubActionsDashboard.Api.Serialisation;

public class StringEnumJsonConverter<T> : JsonConverter<StringEnum<T>> where T : struct, Enum
{
    public override StringEnum<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return new StringEnum<T>(value ?? String.Empty);
    }

    public override void Write(Utf8JsonWriter writer, StringEnum<T> value, JsonSerializerOptions options)
    {
        // Handle null or empty values gracefully
        var stringValue = value.StringValue ?? String.Empty;
        writer.WriteStringValue(stringValue);
    }
}
