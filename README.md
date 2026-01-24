<p align="center">
  <img src="assets/logo.png" alt="FlashMapper Logo" width="300">
</p>

# ⚡ FlashMapper

**High-performance object mapper for .NET with zero reflection and zero runtime overhead.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/FlashMapper.svg)](https://www.nuget.org/packages/FlashMapper/)

FlashMapper uses **Source Generators** to generate mapping code at compile-time, delivering performance comparable to hand-written mapping code while maintaining a clean, AutoMapper-like syntax.

## ✨ Simple & Easy to Use

FlashMapper was designed with **simplicity in mind**. If you know AutoMapper, you already know FlashMapper! The API is intuitive and straightforward - just define your mappings and start using them immediately. No complex configuration, no learning curve.

```csharp
// Configure once
Mapper.CreateMap<User, UserDto>();

// Use anywhere
var dto = Mapper.Map<UserDto>(user);
```

## 🚀 Key Features

- ⚡ **Zero Reflection** - All mapping code is generated at compile-time
- 🎯 **Zero Runtime Configuration** - No configuration overhead or startup cost
- 🔥 **Maximum Performance** - Performance identical to hand-written mapping
- 💪 **Type-Safe** - Compile-time type checking catches errors early
- 🪶 **Zero Allocations** - Uses `Span<T>` internally for optimal memory usage
- 🎨 **Familiar Syntax** - API similar to AutoMapper
- 🔧 **No Dependencies** - No IoC container required
- 📦 **Small Footprint** - Minimal runtime assembly size
- 🎯 **Simple Layout** - Clean, intuitive API that's easy to learn and use

## 📦 Installation

```bash
dotnet add package FlashMapper
```

Or via NuGet Package Manager:

```powershell
Install-Package FlashMapper
```

## 🎯 Supported Frameworks

- .NET 6 (LTS)
- .NET 8 (LTS)
- .NET 9
- .NET 10 (LTS)

## 🔥 Quick Start

### 1. Define Your Models

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 2. Configure Mappings

```csharp
using FlashMapper;

public static class MappingConfiguration
{
    [MapperConfiguration]
    public static void Configure()
    {
        Mapper.CreateMap<User, UserDto>();
    }
}
```

### 3. Use the Mapper

```csharp
var user = new User 
{ 
    Id = 1, 
    Name = "John Doe", 
    Email = "john@example.com" 
};

// Map to new instance
var dto = Mapper.Map<UserDto>(user);

// Or map to existing instance
var existingDto = new UserDto();
Mapper.Map(user, existingDto);
```

## 📚 Advanced Usage

### Reverse Mapping

```csharp
[MapperConfiguration]
public static void Configure()
{
    Mapper.CreateMap<User, UserDto>()
        .ReverseMap(); // Also creates UserDto -> User mapping
}
```

### Custom Property Mapping

```csharp
[MapperConfiguration]
public static void Configure()
{
    Mapper.CreateMap<User, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
        .ForMember(dest => dest.IsActive, opt => opt.Ignore());
}
```

### Nested Object Mapping

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public CustomerDto Customer { get; set; }
}

[MapperConfiguration]
public static void Configure()
{
    Mapper.CreateMap<Order, OrderDto>();
    Mapper.CreateMap<Customer, CustomerDto>();
}
```

### Collection Mapping

```csharp
var users = new List<User> { /* ... */ };

// Map collection
var dtos = users.Select(u => Mapper.Map<UserDto>(u)).ToList();
```

### Using Attributes

```csharp
public class UserDto
{
    public int Id { get; set; }
    
    [MapFrom("FirstName")]
    public string Name { get; set; }
    
    [IgnoreMap]
    public string InternalField { get; set; }
}
```

## 🏎️ Performance Comparison

FlashMapper is designed to match hand-written mapping performance:

### Map to New Instance

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| ManualMapping | 12.79 ns | 1.00x | 120 B |
| **FlashMapper** | **14.41 ns** | **1.13x** | **120 B** |
| Mapperly | 13.55 ns | 1.06x | 120 B |
| Mapster | 20.70 ns | 1.62x | 120 B |
| AutoMapper | 46.18 ns | 3.61x | 120 B |

### Map to Existing Instance (Zero Allocation)

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| **FlashMapper** | **8.81 ns** | **0.69x** | **0 B** |
| ManualMapping | 9.60 ns | 0.75x | 0 B |
| AutoMapper | 37.64 ns | 2.94x | 0 B |

*Benchmarks on .NET 9.0.11, Intel Core i5-14600KF, Windows 11*

**Key Takeaways:**
- ⚡ **FlashMapper_MapToExisting** is the **fastest** - even faster than manual mapping!
- 🎯 Only **~12% slower** than hand-written code for new instances
- 💨 **3.2x faster** than AutoMapper, **1.4x faster** than Mapster
- 🧹 **Zero allocations** when mapping to existing instances

To run benchmarks:

```bash
cd tests/FlashMapper.Benchmarks
dotnet run -c Release
```

## 🛠️ How It Works

1. **Compile-Time Analysis**: The Source Generator analyzes your `[MapperConfiguration]` methods
2. **Code Generation**: Generates optimized mapping methods as partial classes
3. **Zero Overhead**: Generated code is as fast as hand-written mapping
4. **Span<T> Optimization**: Uses `Span<T>` internally for collections and strings (never exposed in public API)

### Generated Code Example

Your configuration:
```csharp
Mapper.CreateMap<User, UserDto>();
```

Generated code:
```csharp
public static partial class Mapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UserDto Map(User source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return new UserDto
        {
            Id = source.Id,
            Name = source.Name,
            Email = source.Email,
        };
    }
}
```

## 🎓 Design Philosophy

### Core Principles

1. **Performance First**: Zero reflection, zero runtime overhead
2. **Compile-Time Safety**: Catch errors at compile-time, not runtime
3. **Predictable Behavior**: No magic, no surprises
4. **Simple API**: Familiar syntax for easy adoption
5. **No Hidden Costs**: No DI, no configuration overhead

### Why Not AutoMapper?

AutoMapper is excellent for flexibility, but:
- Uses reflection (slower)
- Runtime configuration overhead
- Dynamic behavior can be unpredictable
- Higher memory allocations

FlashMapper trades some flexibility for **maximum performance** and **compile-time safety**.

## 📖 API Reference

### Attributes

- `[MapperConfiguration]` - Marks a method as mapper configuration
- `[MapFrom("PropertyName")]` - Maps from a different source property
- `[IgnoreMap]` - Ignores a property during mapping

### Core API

- `Mapper.CreateMap<TSource, TDestination>()` - Creates a mapping configuration
- `Mapper.Map<TDestination>(source)` - Maps to a new instance
- `Mapper.Map<TSource, TDestination>(source, destination)` - Maps to existing instance

### Configuration Methods

- `.ReverseMap()` - Creates bidirectional mapping
- `.ForMember(dest => dest.Property, opt => ...)` - Configures property mapping
- `opt.MapFrom(src => src.Property)` - Custom source property
- `opt.Ignore()` - Ignores property

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Roadmap

- [ ] Advanced collection mapping (Dictionary, HashSet, etc.)
- [ ] Value converters and type adapters
- [ ] Conditional mapping
- [ ] Before/after mapping hooks
- [ ] Deep cloning support
- [ ] Analyzer for common mistakes
- [ ] Performance analyzer

## 💡 Inspiration

Inspired by AutoMapper's elegant API and Mapperly's Source Generator approach.

## 📞 Support

- 🐛 [Report a bug](https://github.com/flashmapper/flashmapper/issues)
- 💡 [Request a feature](https://github.com/flashmapper/flashmapper/issues)
- 💬 [Ask a question](https://github.com/flashmapper/flashmapper/discussions)

---

Made with ⚡ by the FlashMapper contributors
