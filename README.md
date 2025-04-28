# SmartEnum

A lightweight alternative to traditional `enum`, offering a rich typed structure with `Text`, `Code`, `Value` and JSON serialization support.

## Features

- Auto-registration of all instances into a static list.
- Strong typed search by `Value`, `Text`, or `Code`.
- Custom equality (`==`, `!=`) between enum instances, strings, and integers.
- Native JSON serialization and deserialization support.
- Dynamic instance injection at runtime (useful for testing or extending sets dynamically).
- Friendly and extensible code base.

## Installation

Just copy the `SmartEnum.cs` file into your project.  
No external dependency required.

Compatible with **.NET 6+, .NET 7, .NET 8**.

## Basic Usage

```csharp
public sealed class Color : SmartEnum<Color>
{
    public static readonly Color Red = new(1, "Red", "R");
    public static readonly Color Blue = new(2, "Blue", "B");
    public static readonly Color Green = new(3, "Green", "G");

    private Color(int value, string text, string? code = null) : base(value, text, code) { }
}

// Access
var colors = Color.List;

// Find
var green = Color.FromCode("G");

// Compare
if (green == "G") { ... }
if (green == 3) { ... }

// JSON
var json = green.ToJson();
var restored = Color.FromJson(json);
```

It can also be used closer to a real enum, with auto-fill Value and Text:

```csharp
public sealed class Color : SmartEnum<Color>
{
    public static readonly Color Red = new(); // Value = 0 ; Text = "Red" ; Code = null
    public static readonly Color Blue = new(); // Value = 1 ; Text = "Blue" ; Code = null
    public static readonly Color Green = new(); // Value = 2 ; Text = "Green" ; Code = null
}
```
