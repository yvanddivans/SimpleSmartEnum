# SimpleSmartEnum

A lightweight alternative to traditional `enum`, offering a rich typed structure with `Text`, `Code`, `Value`, and JSON serialization support.

## Features

- Auto-registration of all instances into a static list.
- Strong typed search by `Value`, `Text`, or `Code`.
- Custom equality (`==`, `!=`) between enum instances, strings, and integers.
- Native JSON serialization and deserialization support.
- Dynamic instance injection at runtime (useful for testing or extending sets dynamically).
- Friendly and extensible code base.

## Installation

Just copy the `SmartEnum.cs` file into your project.  
No external dependencies required.

Compatible with **.NET 6+, .NET 7, .NET 8**.  
> Minimum Target Framework: **.NET 8**  
> (But can be easily adapted for .NET 6 / .NET 7 if necessary.)

---

## Basic Usage

### 1. Declare your SmartEnum

```csharp
public sealed class Color : SmartEnum<Color>
{
    public static readonly Color Red = new(1, "Red", "R");
    public static readonly Color Blue = new(2, "Blue", "B");
    public static readonly Color Green = new(3, "Green", "G");

    private Color(int value, string text, string? code = null) : base(value, text, code) { }
}
```

- `Value`: an integer value.
- `Text`: a descriptive string.
- `Code`: a short code (optional).

Access all instances:

```csharp
var colors = Color.List;
```

---

### 2. Search and Compare

Find an instance:

```csharp
var green = Color.FromCode("G");
```

Compare instances to strings or integers:

```csharp
if (green == "G") { /* ... */ }
if (green == 3) { /* ... */ }
```

---

### 3. Serialize to JSON

Serialize and deserialize easily:

```csharp
var json = green.ToJson();
var restored = Color.FromJson(json);
```

---

### 4. Dynamically Add Instances

> 🛠️ **Pro Tip:** You can dynamically inject new values at runtime for testing or extension purposes without modifying the source code.

```csharp
Color.Inject(54, "OtherColor");
```

---

### 5. Use Reflection

You can interact with SmartEnum types dynamically:

```csharp
var colorType = typeof(Color);

if (SmartEnum.IsSmartEnum(colorType))
{
    var colors = SmartEnum.GetList(colorType)?.OrderBy(e => e.Text);
    if (colors != null)
    {
        foreach (var color in colors)
        {
            Console.WriteLine($"{color.Code}: {color.ToString(SmartEnum.Format.TextValue)}");
            // Output:
            // "B: Blue (2)"
            // "G: Green (3)"
            // "R: Red (1)"
        }
    }
}
```

---

## Short Declaration with Default Constructor

Default constructor automatically fills `Value` and `Text` based on field names for shorter declarations.

```csharp
public sealed class Color : SmartEnum<Color>
{
    public static readonly Color Red = new();       // Value = 0 ; Text = "Red" ; Code = null
    public static readonly Color PaleBlue = new();   // Value = 1 ; Text = "PaleBlue" ; Code = null
    public static readonly Color Green = new();      // Value = 2 ; Text = "Green" ; Code = null
}
```

---

# License

This project is released under the [MIT License](LICENSE).

---

# Author

Made with ❤️ to simplify enum usage in modern .NET applications.

---

## Notes

- If you need more advanced features like hierarchical enums or localization, consider extending the base class.
- Pull requests and suggestions are welcome!
