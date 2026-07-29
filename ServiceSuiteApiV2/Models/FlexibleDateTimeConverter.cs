using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceSuiteApiV2.Models
{
    // The SpinWebhook vendor sends timestamps as "yyyy-MM-dd HH:mm:ss" (space-separated, not ISO 8601),
    // which System.Text.Json's default DateTime converter rejects outright.
    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s)) return null;

            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }
    }
}
