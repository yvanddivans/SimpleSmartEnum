
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleSmartEnum;

public abstract class SmartEnum(int value, string text, string? code)
{
    public enum Format { TextValue, TextCode, CodeTextValue };
    public int Value { get; } = value;
    public string Text { get; } = text;
    public string? Code { get; } = code;

    public static bool IsSmartEnum(Type type) =>
        type.BaseType?.IsGenericType == true &&
        type.BaseType.GetGenericTypeDefinition() == typeof(SmartEnum<>);

    public static IReadOnlyCollection<SmartEnum>? GetList(Type type)
    {
        if (IsSmartEnum(type))
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            var prop = type.GetProperty("List", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return prop?.GetValue(null) as IReadOnlyCollection<SmartEnum>;
        }
        return default;
    }

    public override string ToString() => Text;
    public string ToString(Format format)
    {
        if (format == Format.TextValue)
            return ToString("{Text} ({Value})");
        else if (format == Format.TextCode)
            return ToString("{Text} ({Code})");
        else if (format == Format.CodeTextValue)
            return ToString("{Code}-{Text} ({Value})");
        else
            throw new ArgumentException($"Format '{format}' unsupported!", nameof(format));
    }
    public string ToString(string format)
    {
        if (format == null) return ToString();
        return format
            .Replace($"{{{nameof(Value)}}}", Value.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace($"{{{nameof(Text)}}}", Text, StringComparison.OrdinalIgnoreCase)
            .Replace($"{{{nameof(Code)}}}", Code, StringComparison.OrdinalIgnoreCase);
    }
}
public abstract class SmartEnum<T> : SmartEnum where T : SmartEnum<T>
{
    public static IReadOnlyCollection<T> List => _instances.AsReadOnly();
    private static readonly List<T> _instances = [];

    private static JsonSerializerOptions? _JsonSerOpts;
    private static JsonSerializerOptions JsonSerOpts => _JsonSerOpts ??= new JsonSerializerOptions()
    {
        Converters = { new SmartEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    /// <summary>
    /// Default base CTOR.
    /// </summary>
    /// <param name="value">If ignored, auto-increments from the biggest Value present if any, or zero.</param>
    /// <param name="text">Defines a Text value for the SmartEnum.</param>
    /// <param name="code">Devines an optionnal Code value for the SmartEnum.</param>
    /// <param name="autoName">If Text is ignored, it will have the CallerMemberName.</param>
    /// <exception cref="ArgumentException">If the added Value already exists in List.</exception>
    public SmartEnum(int? value = default, string? text = default, string? code = default, [CallerMemberName] string? autoName = default)
        : base(value ?? (_instances.Count > 0 ? _instances.Max(i => i.Value) + 1 : 0), text ?? autoName ?? "", code)
    {
        if (_instances.Any(i => i.Value == Value))
            throw new ArgumentException($"Value {Value} already exists in SmartEnum '{typeof(T).Name}'!", nameof(value));
        _instances.Add((T)this);
    }

    /// <summary>
    /// Allows to dynamically inject a virtual instance to List without a declarative Member for flexibility.
    /// </summary>
    /// <returns>Injected object</returns>
    public static T Inject(int? value = default, string? text = default, string? code = default) =>
        (T)Activator.CreateInstance(typeof(T), BindingFlags.NonPublic | BindingFlags.Instance, null, [value, text, code], null)!;

    // Conversions
    public static T? FromValue(int value) => _instances.FirstOrDefault(x => x.Value == value);
    public static T? FromText(string text) => _instances.FirstOrDefault(x => x.Text == text);
    public static T? FromCode(string code) => _instances.FirstOrDefault(x => x.Code == code);
    public static T? Parse(object obj) { return _instances.FirstOrDefault(e => e == obj); }
    public static bool TryParse(object obj, out T? result) { result = Parse(obj); return result is not null; }

    public string? ToJson() => JsonSerializer.Serialize(this, JsonSerOpts);
    public static T? FromJson(string jsonValue) => (T?)JsonSerializer.Deserialize<SmartEnum<T>>(jsonValue, JsonSerOpts);

    // Overrides
    public override int GetHashCode() => HashCode.Combine(GetType(), Value);
    public override bool Equals(object? obj) =>
        obj is SmartEnum<T> objSmartEnum &&
        GetType() == objSmartEnum.GetType() &&
        Value == objSmartEnum.Value;

    // Equality operators supporting SmartEnum, Int32 and String, auto-comparing Values, Texts et Codes.
    public static bool operator ==(SmartEnum<T> leftArgEnum, object rightArgObj)
    {
        if (leftArgEnum is not null)
        {
            if (rightArgObj is SmartEnum<T>)
                return leftArgEnum.Equals(rightArgObj);
            else if (rightArgObj is int)
                return leftArgEnum.Value.Equals(rightArgObj);
            else if (rightArgObj is string)
            {
                return
                    leftArgEnum.Text?.Equals((string)rightArgObj, StringComparison.InvariantCultureIgnoreCase) ??
                    leftArgEnum.Code?.Equals((string)rightArgObj, StringComparison.InvariantCultureIgnoreCase) ??
                    false;
            }
        }
        return rightArgObj is null;
    }
    public static bool operator !=(SmartEnum<T> leftArgEnum, object rightArgObj) { return !(leftArgEnum == rightArgObj); }
    public static bool operator ==(object leftArgObj, SmartEnum<T> rightArgEnum) { return rightArgEnum == leftArgObj; }
    public static bool operator !=(object leftArgObj, SmartEnum<T> rightArgEnum) { return !(rightArgEnum == leftArgObj); }

    private class SmartEnumConverter : JsonConverter<SmartEnum<T>>
    {
        public override SmartEnum<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                return SmartEnum<T>.Parse(str ?? "");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var value = reader.GetInt32();
                return SmartEnum<T>.Parse(value);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);

                var value = doc.RootElement.TryGetProperty(nameof(SmartEnum.Value), out var v) ? v.GetInt32() : (int?)null;
                if (value.HasValue) return SmartEnum<T>.FromValue(value.Value);

                var code = doc.RootElement.TryGetProperty(nameof(SmartEnum.Code), out var c) ? c.GetString() : null;
                if (!string.IsNullOrWhiteSpace(code)) return SmartEnum<T>.FromCode(code);

                var text = doc.RootElement.TryGetProperty(nameof(SmartEnum.Text), out var t) ? t.GetString() : null;
                if (!string.IsNullOrWhiteSpace(text)) return SmartEnum<T>.FromText(text);
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, SmartEnum<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(value.Text), value.Text);
            writer.WriteString(nameof(value.Code), value.Code);
            writer.WriteNumber(nameof(value.Value), value.Value);
            writer.WriteEndObject();
        }
    }
}
