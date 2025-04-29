
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
    public static IReadOnlyCollection<T> List
    {
        get
        {
            RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
            return _instances.AsReadOnly();
        }
    }
    private static readonly List<T> _instances = [];

    private static JsonSerializerOptions? _JsonSerOpts;
    private static JsonSerializerOptions JsonSerOpts => _JsonSerOpts ??= new JsonSerializerOptions()
    {
        Converters = { new SmartEnumJsonConverterFactory() },
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
    public static T? Parse(object obj) => List.FirstOrDefault(e => e.Equals(obj));
    public static bool TryParse(object obj, out T? result) { result = Parse(obj); return result is not null; }

    public string? ToJson() => JsonSerializer.Serialize(this, JsonSerOpts);
    public static T? FromJson(string jsonValue) => (T?)JsonSerializer.Deserialize<SmartEnum<T>>(jsonValue, JsonSerOpts);

    // Overrides
    public override int GetHashCode() => HashCode.Combine(GetType(), Value);
    public override bool Equals(object? obj)
    {
        if (obj is SmartEnum<T> enumObj)
            return Value == enumObj.Value;
        else if (obj is int intObj)
            return Value == intObj;
        else if (obj is string strObj)
        {
            return
                Text?.Equals(strObj, StringComparison.InvariantCultureIgnoreCase) ??
                Code?.Equals(strObj, StringComparison.InvariantCultureIgnoreCase) ??
                false;
        }
        return base.Equals(obj);
    }

    // Equality operators supporting SmartEnum, Int32 and String, auto-comparing Values, Texts et Codes.
    public static bool operator ==(SmartEnum<T> leftArgEnum, object rightArgObj) { return leftArgEnum?.Equals(rightArgObj) ?? rightArgObj is null; }
    public static bool operator !=(SmartEnum<T> leftArgEnum, object rightArgObj) { return !(leftArgEnum == rightArgObj); }
    public static bool operator ==(object leftArgObj, SmartEnum<T> rightArgEnum) { return rightArgEnum == leftArgObj; }
    public static bool operator !=(object leftArgObj, SmartEnum<T> rightArgEnum) { return !(rightArgEnum == leftArgObj); }
}
