using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleSmartEnum;

public class SmartEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return SmartEnum.IsSmartEnum(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // Trouve le T dans SmartEnum<T>
        var enumType = typeToConvert;
        var converterType = typeof(SmartEnumJsonConverter<>).MakeGenericType(enumType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
public class SmartEnumJsonConverter<TSmartEnum> : JsonConverter<TSmartEnum>
    where TSmartEnum : SmartEnum<TSmartEnum>
{
    public override TSmartEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return SmartEnum<TSmartEnum>.Parse(str ?? "");
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return SmartEnum<TSmartEnum>.Parse(value);
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);

            var value = doc.RootElement.TryGetProperty(nameof(SmartEnum.Value), out var v) ? v.GetInt32() : (int?)null;
            if (value.HasValue) return SmartEnum<TSmartEnum>.FromValue(value.Value);

            var code = doc.RootElement.TryGetProperty(nameof(SmartEnum.Code), out var c) ? c.GetString() : null;
            if (!string.IsNullOrWhiteSpace(code)) return SmartEnum<TSmartEnum>.FromCode(code);

            var text = doc.RootElement.TryGetProperty(nameof(SmartEnum.Text), out var t) ? t.GetString() : null;
            if (!string.IsNullOrWhiteSpace(text)) return SmartEnum<TSmartEnum>.FromText(text);
        }
        return default;
    }
    public override void Write(Utf8JsonWriter writer, TSmartEnum value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(value.Text), value.Text);
        writer.WriteString(nameof(value.Code), value.Code);
        writer.WriteNumber(nameof(value.Value), value.Value);
        writer.WriteEndObject();
    }
}
