
# BindMapper

**BindMapper** is a high-performance object-to-object mapper for .NET, powered by **Source Generators**. It generates optimized mapping code at compile-time, eliminating reflection overhead and delivering performance comparable to hand-written code.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/BindMapper.svg)](https://www.nuget.org/packages/BindMapper/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BindMapper.svg)](https://www.nuget.org/packages/BindMapper/)
[![.NET](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)

---

## 🎯 What Problem Does It Solve?

| Scenario | AutoMapper | Mapster | BindMapper |
|----------|-----------|---------|-----------|
| **Performance** | 37.8 ns | 19.2 ns | **12.0 ns** ⚡ |
| **Memory** | High GC pressure | Medium | **Near-zero** |
| **Setup complexity** | High (profiles, configurations) | Medium | **Minimal** |
| **Reflection** | Yes (runtime) | Yes (IL emit) | **None (compile-time)** |

**Best for**: High-scale systems, microservices, performance-critical paths, low-latency applications.

---

## 📦 Installation

```bash
dotnet add package BindMapper
```

**Supported frameworks**: .NET 6, 8, 9, 10

---

## 🚀 Quick Start

### Step 1: Define Models

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

### Step 2: Configure Mappings (Compile-Time)

Create a static method decorated with `[MapperConfiguration]`. This is analyzed at compile-time **only** — the method is never executed at runtime.

```csharp
using BindMapper;

public class MappingConfiguration
{
    /// This method is NOT executed at runtime — it's analyzed at compile-time by the Source Generator.
    [MapperConfiguration]
    public static void ConfigureMappings()
    {
        // Source Generator extracts these calls at compile-time
        MapperSetup.CreateMap<User, UserDto>();
    }
}
```

### Step 3: Use at Runtime

```csharp
var user = new User { Id = 1, Name = "John", Email = "john@email.com" };

// Create new instance (⚡ 12 ns)
var dto = Mapper.To<UserDto>(user);

// Update existing instance — zero allocation (⚡ 10 ns)
var existingDto = new UserDto();
Mapper.To(user, existingDto);
```

---

## 📚 Complete API Reference

### Single Object Mapping

| Method | Purpose | Allocation | Performance |
|--------|---------|-----------|-------------|
| `Mapper.To<TDest>(source)` | Map to new instance | DTO size | ⚡⚡⚡ ~12 ns |
| `Mapper.To<TDest>(source, dest)` | Map to existing instance | 0 bytes | ⚡⚡⚡ ~10 ns (zero-alloc) |

**Example:**

```csharp
// ✅ Create new DTO
var userDto = Mapper.To<UserDto>(user);

// ✅ Reuse existing instance (zero allocation)
var cachedDto = new UserDto();
Mapper.To(user, cachedDto);  // Updates cachedDto in-place
```

### Collection Mapping

BindMapper provides **multiple APIs** for different scenarios. All are optimized with `Span<T>` on .NET 8+.

#### `MapList<TSource, TDestination>(List<T>, Func)` — To List

```csharp
List<User> users = GetUsers();

// Maps List → List using Span optimization on .NET 8+
var dtos = Mapper.MapList(users, user => Mapper.To<UserDto>(user));
```

**Performance:**
- .NET 8+: Uses `CollectionsMarshal.AsSpan()` for zero-allocation enumeration
- .NET 6-7: Uses standard foreach
- **Time:** ~1.2 μs for 100 items

---

#### `MapArray<TSource, TDestination>(T[], Func)` — To Array

```csharp
User[] users = GetUsersArray();

// Maps Array → Array using Span
var dtos = Mapper.MapArray(users, user => Mapper.To<UserDto>(user));
```

**Performance:**
- Uses `Span<T>` directly for zero-copy iteration
- **Time:** ~1.3 μs for 100 items

---

#### `MapEnumerable<TSource, TDestination>(IEnumerable<T>, Func)` — Auto-Detection

Intelligently detects source type and chooses the fastest path:

```csharp
IEnumerable<User> users = GetUsers(); // Could be List, Array, or any IEnumerable

// Source Generator picks the best path:
// - List<T> → uses MapList (Span-optimized)
// - T[] → uses MapArray (Span-optimized)
// - ICollection<T> → uses optimized enumeration
// - IEnumerable<T> → fallback with foreach
var dtos = Mapper.MapEnumerable(users, user => Mapper.To<UserDto>(user));
```

**Performance:** Variable, but auto-optimized
- List/Array: ~1.2-1.3 μs for 100 items
- Generic IEnumerable: ~2.0 μs for 100 items

---

#### `MapSpan<TSource, TDestination>(ReadOnlySpan<T>, Func)` — Maximum Performance

**WARNING**: Destination Span must be pre-allocated and large enough!

```csharp
ReadOnlySpan<User> users = new User[] { /* ... */ };
Span<UserDto> destination = stackalloc UserDto[users.Length];

// TRUE zero allocation — only stack memory
Mapper.MapSpan(users, user => Mapper.To<UserDto>(user));

// ⚠️ DO NOT do this:
// Span<UserDto> buffer = stackalloc UserDto[1000]; // Stack overflow risk!
```

**Performance:** ⚡⚡⚡ Fastest — true zero heap allocation
**Use case:** Performance-critical loops, fixed-size batches

---

#### `MapToList<TSource, TDestination>(ICollection<T>, Func)` — Legacy API

Still available for compatibility. **Prefer `MapList` or `MapEnumerable`** for new code.

```csharp
ICollection<User> users = GetUsers();
var dtos = Mapper.MapToList(users, user => Mapper.To<UserDto>(user));
```

---

#### `MapToArray<TSource, TDestination>(ICollection<T>, Func)` — Legacy API

Still available for compatibility. **Prefer `MapArray` or `MapEnumerable`** for new code.

```csharp
ICollection<User> users = GetUsers();
var dtos = Mapper.MapToArray(users, user => Mapper.To<UserDto>(user));
```

---

## ⚙️ Advanced Configuration

### Custom Member Mapping with `ForMember`

Use `ForMember` to customize how individual properties are mapped:

```csharp
[MapperConfiguration]
public static void ConfigureMappings()
{
    MapperSetup.CreateMap<Product, ProductDto>()
        // Map differently-named property
        .ForMember(
            dest => dest.DisplayName,
            opt => opt.MapFrom(src => src.Name))
        
        // Custom transformation
        .ForMember(
            dest => dest.TotalPrice,
            opt => opt.MapFrom(src => src.Price * src.Quantity))
        
        // Ignore property (won't be mapped)
        .ForMember(
            dest => dest.InternalId,
            opt => opt.Ignore());
}
```

### Using Attributes for Configuration

Annotate your DTO properties to customize mapping behavior:

```csharp
public class ProductDto
{
    public int Id { get; set; }
    
    [MapFrom("ProductName")]  // Map from differently-named source property
    public string Name { get; set; }
    
    [IgnoreMap]  // Skip during mapping
    public string CacheKey { get; set; }
    
    public decimal Price { get; set; }
}
```

### Reverse Mapping

Create bidirectional mappings:

```csharp
[MapperConfiguration]
public static void ConfigureMappings()
{
    MapperSetup.CreateMap<User, UserDto>()
        .ReverseMap();  // Also enables UserDto → User mapping
}
```

⚠️ **Requirements for ReverseMap**:
- Destination type must have writable properties (not read-only)
- Both source and destination must have matching properties
- If reverse fails at runtime, check diagnostic [VMAPPER003](#diagnostics-and-errors)

---

## 🔍 How Source Generators Work

The Source Generator analyzes your `[MapperConfiguration]` methods at **compile-time** and generates optimized mapping code.

### Example: What Gets Generated

You write:
```csharp
[MapperConfiguration]
public static void Configure()
{
    MapperSetup.CreateMap<User, UserDto>();
}
```

The generator creates (in generated file `Mapper.g.cs`):
```csharp
public static class Mapper
{
    public static UserDto To(User source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        
        // ✅ Direct assignments — no reflection!
        // ✅ Nested properties handled automatically
        // ✅ Inlineablefor JIT optimization
        return new UserDto
        {
            Id = source.Id,
            Name = source.Name ?? string.Empty,
            Email = source.Email,
        };
    }
}
```

### What This Means

| Aspect | Benefit |
|--------|---------|
| **Compile-time generation** | No runtime reflection overhead |
| **Direct assignments** | CPU cache-friendly code |
| **JIT inlining** | Method calls disappear in Release builds |
| **Null-safety** | Detects potential null-refs at compile-time |
| **Determinism** | Same input = identical output, always |

---

## 🏎️ Performance Characteristics

### Benchmark Results (.NET 10, Intel Core i5-14600KF)

```
Mapper                  Time        Memory      Ratio
──────────────────────────────────────────────────────
Manual hand-written     11.750 ns   120 B       1.00
BindMapper              12.030 ns   120 B       1.02  ⭐
Mapperly               12.285 ns   120 B       1.05
Mapster                19.174 ns   120 B       1.63
AutoMapper             37.854 ns   120 B       3.22
```

### Memory Allocation Patterns

```csharp
// ✅ Zero allocation for object mapping
Mapper.To(source, existingDto);  // 0 bytes

// ⚡ Minimal allocation for collections
Mapper.MapArray(users, x => Mapper.To<UserDto>(x));
// Only allocates: new array + DTOs (expected)

// ⚡⚡⚡ TRUE zero allocation
Span<UserDto> dest = stackalloc UserDto[100];
Mapper.MapSpan(users.AsSpan(), x => Mapper.To<UserDto>(x));
// Stack memory only, no heap allocation
```

### When NOT to Use BindMapper

- ❌ Complex mapping scenarios (use Mapster or AutoMapper)
- ❌ Highly dynamic mappings (known only at runtime)
- ❌ Scenarios requiring reflection-based introspection
- ❌ Projects where compilation speed is critical (SG adds ~50-150ms to build)

---

## ▶️ Source Generator

### How to Configure Mappings

1. Create a **static method** in any **public class**
2. Decorate with **`[MapperConfiguration]`**
3. Call **`MapperSetup.CreateMap<T1, T2>()`** for each mapping

```csharp
// ✅ This works
public class MyMappings
{
    [MapperConfiguration]
    public static void Setup()
    {
        MapperSetup.CreateMap<User, UserDto>();
        MapperSetup.CreateMap<Address, AddressDto>();
    }
}

// ✅ Multiple methods (last one wins for duplicate mappings)
public class UserMappings
{
    [MapperConfiguration]
    public static void ConfigureUsers()
    {
        MapperSetup.CreateMap<User, UserDto>();
    }
}

public class ProductMappings
{
    [MapperConfiguration]  
    public static void ConfigureProducts()
    {
        MapperSetup.CreateMap<Product, ProductDto>();
    }
}

// ❌ This WON'T work (method not static)
public class WrongMappings
{
    [MapperConfiguration]
    public void Setup()  // ERROR: must be static!
    {
        MapperSetup.CreateMap<User, UserDto>();
    }
}

// ❌ This WON'T work (CreateMap outside [MapperConfiguration])
public class AnotherWrongApproach
{
    public void Setup()
    {
        MapperSetup.CreateMap<User, UserDto>();  // IGNORED by SG
    }
}
```

### Generated Code Location

After building, find generated code in:
```
bin/Debug/net8.0/BindMapper.g.cs
```

Inspect this file to see exactly what code was generated.

---

## 🚦 Diagnostics and Errors

BindMapper emits compiler warnings/errors to guide you:

### VMAPPER001: Missing Source Property
```
Source property not found for mapping to 'Email'.  
Source type 'User' does not have property 'Email'.
This property will not be mapped, resulting in default value.
```

**Action**: Either add the property to source, or use `.ForMember(...).Ignore()`.

---

### VMAPPER002: Duplicate Mapping
```
Mapping from 'User' to 'UserDto' is already configured in another [MapperConfiguration] method.
The previous configuration will be overridden.
```

**Action**: Consolidate your mappings into a single `[MapperConfiguration]` method, or rename to distinguish them.

---

### VMAPPER003: ReverseMap with Read-Only Properties
```
ReverseMap configured for 'User' -> 'UserDto', but destination type has read-only properties: Id.
These properties cannot be assigned, causing mapping to fail at runtime.
```

**Action**: Either remove `.ReverseMap()` or make the properties writable.

---

### VMAPPER004: Invalid ForMember Syntax
```
ForMember configuration for destination property could not be parsed.
Ensure you use lambda expressions: .ForMember(d => d.Prop, opt => opt.MapFrom(s => s.Source))
```

**Action**: Fix the lambda expression syntax or the Source Generator cannot analyze it.

---

### VMAPPER005: Unvalidated Expression
```
The MapFrom expression for property 'Total' uses complex lambda syntax that cannot be validated at compile-time.
Ensure the expression is valid and returns a type assignable to the destination property.
```

**Action**: Simplify the expression or verify it's correct. The generator couldn't statically analyze it.

---

### VMAPPER006: CreateMap Outside Configuration
```
CreateMap<User, UserDto>() should only be called within methods decorated with [MapperConfiguration].
This call will be ignored by the source generator.
```

**Action**: Move your `CreateMap` call into a method decorated with `[MapperConfiguration]`.

---

### VMAPPER007: Type Mismatch
```
Cannot automatically convert source property 'Price' of type 'decimal' to destination property 'Price' of type 'string'.
Consider using .MapFrom() to provide custom conversion logic.
```

**Action**: Use `.ForMember(...).MapFrom(src => src.Price.ToString())` to handle the conversion.

---

### VMAPPER008: Implicit Numeric Conversion
```
Source property 'Age' is type 'decimal' but destination property 'Age' is type 'int'.
This implicit conversion may result in data loss.
Consider using explicit .MapFrom() with proper conversion logic.
```

**Action**: Use `.ForMember(d => d.Age, opt => opt.MapFrom(s => (int)s.Age))`.

---

### VMAPPER009: Non-Nullable Without Source
```
Destination property 'Email' of type 'string' is non-nullable, but no source property 'Email' exists.
This will result in a NullReferenceException at runtime.
```

**Action**: Add the property to source type, or use `.ForMember(...).MapFrom(s => "default")`.

---

## ⚠️ Important Notes

### Thread Safety

- `Mapper` class is thread-safe (no shared state)
- `MapperSetup.CreateMap()` is analyzed at compile-time only — not thread-unsafe
- Generated mapping methods are fully reentrant

### What Happens at Compile-Time vs Runtime

| Operation | When | Details |
|-----------|------|---------|
| Analyze `[MapperConfiguration]` methods | **Compile-time** | SG reads your source code |
| Extract `CreateMap` calls | **Compile-time** | Builds mapping database |
| Generate optimized code | **Compile-time** | Creates `Mapper.g.cs` |
| Execute mapping methods | **Runtime** | Your code calls generated `Mapper.To()` |

**Important**: The `[MapperConfiguration]` method **is never executed at runtime**. It's only analyzed at compile-time.

---

### Nested Mapping

Nested objects are automatically mapped:

```csharp
public class User
{
    public int Id { get; set; }
    public Address Address { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public AddressDto Address { get; set; }
}

[MapperConfiguration]
public static void Configure()
{
    MapperSetup.CreateMap<User, UserDto>();
    MapperSetup.CreateMap<Address, AddressDto>();
    // ^ Both required for nested mapping to work
}

var user = new User { Id = 1, Address = new Address { City = "NYC" } };
var dto = Mapper.To<UserDto>(user);
// ✅ dto.Address is automatically mapped!
```

---

### Property Mapping Rules

```
Source Property            Destination Property       Result
─────────────────────────  ──────────────────────────  ──────
Exists, matches by name    Matches                     ✅ Mapped
Exists, different name     Uses [MapFrom] attribute    ✅ Mapped
Exists                     [IgnoreMap] on dest        ❌ Ignored
Exists                     No matching dest            ✅ Silent ignore
Null                       Not nullable               ⚠️ VMAPPER009
                           Read-only                  ❌ Cannot assign
```

---

## 🐛 Common Issues and Solutions

### Issue: "No mapping configured from 'User' to 'UserDto'"

**Cause**: `[MapperConfiguration]` not found or not called.

**Solution**:
```csharp
// ✅ Make sure you have a [MapperConfiguration] method:
[MapperConfiguration]
public static void Configure()
{
    MapperSetup.CreateMap<User, UserDto>();
}

// ❌ DON'T do this:
// Mapper.CreateMap<User, UserDto>();  // Wrong! Use MapperSetup.
```

---

### Issue: Generated code not recognizing my mapping

**Cause**: `[MapperConfiguration]` method is not public or not static.

**Solution**:
```csharp
// ✅ Correct
[MapperConfiguration]
public static void Configure()
{
    MapperSetup.CreateMap<User, UserDto>();
}

// ❌ Wrong: method not public
[MapperConfiguration]
private static void Configure() { }

// ❌ Wrong: method not static
[MapperConfiguration]
public void Configure() { }
```

---

### Issue: Mapping is slow

**Possible causes**:
1. Using `MapEnumerable` with large `IEnumerable` (generic path)
   - **Solution**: Use `MapList` or `MapArray` if possible
2. Calling `Mapper.To()` in nested foreach loops
   - **Solution**: Use `MapList()` or `MapArray()` on outer collection first
3. Using `ReverseMap()` excessively
   - **Solution**: Create separate forward and reverse mappings if needed

---

### Issue: PropertyNotFoundException on null source

**Cause**: Passing null to `Mapper.To<T>(null)`

**Solution**:
```csharp
// ❌ This throws ArgumentNullException
var dto = Mapper.To<UserDto>(null);

// ✅ Check for null first
var dto = user != null ? Mapper.To<UserDto>(user) : null;

// ✅ Or use null-coalescing in collection mapping
var dtos = Mapper.MapList(users, u => u != null ? Mapper.To<UserDto>(u) : null);
```

---

## 📋 Comparison Table

| Feature | BindMapper | AutoMapper | Mapster | Mapperly |
|---------|-----------|-----------|---------|----------|
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Reflection** | None | Yes | Yes | None |
| **Compilation** | Compile-time SG | Runtime | Runtime IL emit | Compile-time SG |
| **Setup overhead** | None | High | Medium | None |
| **Flexibility** | Good | Excellent | Excellent | Limited |
| **Memory footprint** | Minimal | High | Medium | Minimal |
| **Learning curve** | Easy | Medium | Medium | Easy |
| **Best for** | Performance paths | Complex scenarios | Balanced | Maximum performance |

---

## 📖 Additional Resources

- **Generated Code**: See `bin/Debug/net8.0/BindMapper.g.cs` after build
- **Benchmarks**: [BenchmarkDotNet results](tests/BindMapper.Benchmarks)
- **Examples**: [EXAMPLE_NEW_API.cs](EXAMPLE_NEW_API.cs)
- **API Reference**: [API_REFERENCE.md](API_REFERENCE.md)

---

## 📄 License

MIT License - see [LICENSE](LICENSE)

---

## 🤝 Contributing

Open to issues, questions, and PRs. Please ensure:
- Code follows .NET conventions
- Tests pass (`dotnet test`)
- New features include tests
- Performance regressions are addressed

---

**Made with ❤️ for high-performance .NET**
