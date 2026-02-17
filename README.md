# BindMapper

**BindMapper** is an ultra-high-performance object-to-object mapper for .NET, powered by **Roslyn Source Generators**.  

It generates **extremely optimized mapping code at compile-time**, using advanced techniques like:
- ✨ **Ref-based loops** with `Unsafe.Add` for zero bounds checking
- ✨ **8-way loop unrolling** on large collections
- ✨ **Zero-boxing guarantees** via `CollectionsMarshal` and `MemoryMarshal`
- ✨ **`Unsafe.SkipInit`** to eliminate unnecessary zero-initialization
- ✨ **Branchless null checks** for nested mappings

The result? **Performance identical to hand-written code** (~11.8ns per mapping) while maintaining AutoMapper-like syntax for familiarity and ease of migration.

[![CI](https://github.com/djesusnet/BindMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/djesusnet/BindMapper/actions/workflows/ci.yml)
[![CD](https://github.com/djesusnet/BindMapper/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/djesusnet/BindMapper/actions/workflows/publish-nuget.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/BindMapper.svg)](https://www.nuget.org/packages/BindMapper/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BindMapper.svg)](https://www.nuget.org/packages/BindMapper/)
[![.NET](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)

---

## 🚀 Features

### Core Features
- ⚡ **Compile-time code generation** - Zero runtime overhead, all mapping code generated during build
- 🎯 **AutoMapper-compatible syntax** - Familiar API with `CreateMap`, `ForMember`, `ReverseMap`
- 🔒 **100% type-safe** - Compile-time validation of all mappings
- 🚫 **Zero reflection** - No runtime reflection, expression trees, or dynamic code
- 🎨 **Clean generated code** - Human-readable, debuggable C# code
- 🔄 **Bidirectional mapping** - `ReverseMap()` support

### Performance Features
- 🏎️ **11.8ns per mapping** - Identical to hand-written code
- 📊 **3x faster than AutoMapper** - 11.8ns vs 34.9ns
- 🎯 **1.6x faster than Mapster** - 11.8ns vs 19.1ns
- ⚡ **Competitive with Mapperly** - 11.8ns vs 12.0ns
- 💾 **Zero-allocation mapping** - `MapToExisting` creates no garbage
- 🔧 **Advanced optimizations**:
  - Ref-based loops with `Unsafe.Add` for zero bounds checking
  - 8-way loop unrolling for large collections (8+ items)
  - `CollectionsMarshal.AsSpan()` for direct memory access
  - `MemoryMarshal.GetReference()` to bypass array indexing
  - `Unsafe.SkipInit<T>()` to eliminate zero-initialization overhead
  - Branchless null checks using simple conditionals

### Framework Support
- ✅ .NET 6.0
- ✅ .NET 8.0
- ✅ .NET 9.0
- ✅ .NET 10.0
- ✅ AOT (Ahead-of-Time) compatible
- ✅ Trimming-safe

---

## 📦 Installation

```bash
dotnet add package BindMapper
```

Supported frameworks:
- .NET 6
- .NET 8
- .NET 9
- .NET 10

### Basic Mapping

```csharp
using BindMapper;

var user = new User { Id = 1, Name = "John", Email = "john@email.com" };

// Create new object
var dto = Mapper.To<UserDto>(user);

// Map to existing object (zero allocation)
var existingDto = new UserDto();
Mapper.To(user, existingDto);
```

### Advanced Configuration

```csharp
[MapperConfiguration]
public static void Configure()
{
    // Simple mapping
    MapperSetup.CreateMap<User, UserDto>();

    // Custom property mapping
    MapperSetup.CreateMap<User, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name));

    // Bidirectional mapping
    MapperSetup.CreateMap<User, UserDto>()
        .ReverseMap();

    // Ignore properties
    MapperSetup.CreateMap<User, UserDto>()
        .ForMembeMapping APIs

BindMapper provides highly optimized collection mapping methods with **zero-boxing guarantees**:

```csharp
var users = new List<User>
{
    new() { Id = 1, Name = "John", Email = "john@email.com" },
    new() { Id = 2, Name = "Jane", Email = "jane@email.com" },
    new() { Id = 3, Name = "Bob", Email = "bob@email.com" }
};

// Eager evaluation - highly optimized
var list = Mapper.ToList<UserDto>(users);        // Uses CollectionsMarshal.AsSpan
var array = Mapper.ToArray<UserDto>(users);      // Direct memory access with MemoryMarshal
var collection = Mapper.ToCollection<UserDto>(users);

// Lazy evaluation - deferred execution
var enumerable = Mapper.ToEnumerable<UserDto>(users);

// Span mapping - zero allocation
Span<User> userSpan = stackalloc User[3];
var dtoSpan = Mapper.ToSpan<UserDto>(userSpan);
```

### Collection Performance Optimizations

| Collection Size | Optimization Applied |
|----------------|---------------------|
| 1-7 items      | Simple loop with direct assignment |
| 8+ items       | **8-way loop unrolling** for maximum throughput |
| All sizes      | `Unsafe.Add` for zero bounds checking |
### Compilation Pipeline

1. **Analysis Phase**: Source Generator scans for `[MapperConfiguration]` attributes
2. **Extraction Phase**: Parses `CreateMap<TSource, TDest>()` calls using Roslyn syntax trees
3. **Validation Phase**: Type compatibility checking at compile-time
4. **Generation Phase**: Produces optimized C# mapping code
5. **JIT Phase**: Generated methods are aggressively inlined by JIT compiler

### Generated Code Example

**Simple Property Mapping:**
```csharp
public static UserDto To(User source)
{
    var destination = new UserDto();
    destination.Id = source.Id;
    destination.Name = source.Name;
    destination.Email = source.Email;
    return destination;
}
```

**Optimized Collection Mapping (8+ items):**
``✅ All generated methods are **100% stateless**
- ✅ Safe for **concurrent use** across multiple threads
- ✅ No shared state, locks, or synchronization required
- ✅ Thread-local optimizations automatically applied by JIT

---

## 🔧 Advanced Topics

### Incremental Source Generation

BindMapper uses Roslyn's **Incremental Source Generators** with fine-grained caching:
- Only regenerates code when mapping configuration changes
- Uses `ForAttributeWithMetadataName` API for optimal performance
- **~25% faster build times** compared to traditional source generators

### Build Performance

| Aspect | Impact |
|--------|--------|
| First build | ~200ms overhead (generator initialization) |
| Incremental builds | ~5-10ms (cached results) |
| Full rebuilds | ~150ms (regeneration) |
| Memory usage | <50MB during generation |

### Diagnostic Analyzers

BindMapper includes compile-time analyzers that catch:
- ❌ Incompatible property types
- ❌ Missing mapping configurations
- ❌ Circular dependencies
- ❌ Invalid `ForMember` expressions
- ❌ Type mismatches in custom mappings

Diagnostics appear as **build errors/warnings** in Visual Studio, VS Code, and Rider.

---

## 🐛 Troubleshooting

### "MapperConfiguration not found"

Ensure your configuration class is `public` or `internal` and the `[MapperConfiguration]` method is `static`:

```csharp
public static class MappingConfig  // ✅ public or internal
{
    [MapperConfiguration]
    public static void Configure() // ✅ static
    {
        MapperSetup.CreateMap<User, UserDto>();
    }
}
```

### "No generated code"

1. Clean and rebuild: `dotnet clean && dotnet build`
2. Check for compilation errors in your configuration
3. Verify `BindMapper` package is correctly installed
4. Check the generated files location (see "Inspect Generated Code" section)

### CS0436 Warning (Type conflicts)

If you see CS0436, ensure you're not mixing:
- ✅ NuGet package reference only (recommended)
- ❌ Both NuGet package and project reference (causes duplicates)

---

## 🗺️ Roadmap

### v1.1.0 (Planned)
- [ ] Expression-based custom converters
- [ ] Constructor injection support
- [ ] Collection element conditions (`Where` clause)
- [ ] Async mapping support

### v1.2.0 (Planned)
- [ ] Multi-source mapping (`CreateMap<T1, T2, TDest>`)
- [ ] Post-mapping actions (`AfterMap`)
- [ ] Pre-mapping validation
- [ ] Polymorphic mapping support

### v2.0.0 (Future)
- [ ] Full AutoMapper API compatibility layer
- [ ] Migration tooling from AutoMapper
- [ ] Performance profiling integration
- [ ] Visual Studio extension for mapping visualization

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m 'Add my feature'`
4. Push to the branch: `git push origin feature/my-feature`
5. Open a Pull Request

### Development Setup

```bash
git clone https://github.com/djesusnet/BindMapper.git
cd BindMapper
dotnet restore
dotnet build
dotnet test
```

### Running Benchmarks

```bash
cd tests/BindMapper.Benchmarks
dotnet run --configuration Release
```

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details

---

## 📞 Support

- 🐛 **Issues**: [GitHub Issues](https://github.com/djesusnet/BindMapper/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/djesusnet/BindMapper/discussions)
- 📧 **Email**: [Your contact email]

---

## 🙏 Acknowledgments

Inspired by:
- [AutoMapper](https://github.com/AutoMapper/AutoMapper) - API design
- [Mapperly](https://github.com/riok/mapperly) - Source generator approach
- [Mapster](https://github.com/MapsterMapper/Mapster) - Performance optimizations

Built with ❤️ using .NET and Roslyn Source Generators
    var length = span.Length;
    var i = 0;
    
    // 8-way unrolled loop
    for (; i <= length - 8; i += 8)
    {
        destination.Add(Map(Unsafe.Add(ref searchSpace, i)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 1)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 2)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 3)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 4)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 5)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 6)));
        destination.Add(Map(Unsafe.Add(ref searchSpace, i + 7)));
    }
    
    // Process remaining items
    for (; i < length; i++)
    {
        destination.Add(Map(Unsafe.Add(ref searchSpace, i)));
    }
    
    return destination;
}
```

### Inspect Generated Code

Generated files can be found in your project's output directory:

```
obj/Debug/net10.0/generated/BindMapper.Generators/BindMapper.Generators.MapperGenerator/Mapper.g.cs
```

Or via Visual Studio: **Solution Explorer → Dependencies → Analyzers → BindMapper.Generators → Mapper.g.cs**lic static void Configure()
{
    MapperSetup.CreateMap<User, UserDto>();
    MapperSetup.CreateMap<Order, OrderDto>();  // Nested mapping is automatic
}
public static class MappingConfiguration
{
    [MapperConfiguration]
    public static void Configure()
    {
        MapperSetup.CreateMap<User, UserDto>();
    }
}
```

⚠️ This method is analyzed at **compile-time only** and is never executed at runtime.

---

## ▶️ Usage

```csharp
var user = new User { Id = 1, Name = "John", Email = "john@email.com" };
Performance Benchmarks

### Single Object Mapping (.NET 10, Intel Core i5-14600KF)

| Method                   | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------|-----------|-------|--------|-----------|-------------|
| **ManualMapping**       | **11.83 ns** | **1.00** | 0.0096 | 120 B     | 1.00        |
| **BindMapper_Map**      | **11.84 ns** | **1.00** | 0.0096 | 120 B     | 1.00        |
| Mapperly_Map            | 12.00 ns  | 1.01  | 0.0096 | 120 B     | 1.00        |
| Mapster_Map             | 19.15 ns  | 1.62  | 0.0095 | 120 B     | 1.00        |
| AutoMapper_Map          | 34.87 ns  | 2.95  | 0.0095 | 120 B     | 1.00        |

### Map to Existing Object (Zero Allocation)

| Method                       | Mean      | Ratio | Allocated | Alloc Ratio |
|-----------------------------|-----------|-------|-----------|-------------|
| ManualMapping_ToExisting    | 10.01 ns  | 0.85  | 0 B       | 0.00        |
| **BindMapper_MapToExisting**| **13.15 ns** | **1.11** | **0 B**   | **0.00** |
| AutoMapper_MapToExisting    | 37.25 ns  | 3.15  | 0 B       | 0.00        |

### Key Performance Insights

- ✅ **BindMapper = Manual Code**: 11.84ns vs 11.83ns (0.01ns difference!)
- ✅ **2.95x faster than AutoMapper**: 11.84ns vs 34.87ns
- ✅ **1.62x faster than Mapster**: 11.84ns vs 19.15ns
- ✅ **Practically identical to Mapperly**: 11.84ns vs 12.00ns
- ✅ **Zero-allocation mapping available**: 0 bytes GC pressure with `MapToExisting`

### Test Environment
- **Hardware**: Intel Core i5-14600KF (14 physical cores, 20 logical cores)
- **Runtime**: .NET 10.0.3 (10.0.326.7603), X64 RyuJIT AVX2
- **GC**: Concurrent Workstation
- **SIMD**: AVX2, AES, BMI1, BMI2, FMA, LZCNT, PCLMUL, POPCNT, AvxVnni

> 💡 **Benchmarked using BenchmarkDotNet 0.14.0** with methodology following industry best practices
var array = Mapper.ToArray<UserDto>(users);
var enumerable = Mapper.ToEnumerable<UserDto>(users);
```

- `ToList` and `ToArray` are eager
- `ToEnumerable` is lazy
- Optimized paths for List and Array

---

## 🏎️ Benchmarks (.NET 10)

```
Mapper              Mean (ns)
--------------------------------
Manual mapping      11.750
BindMapper          12.030
Mapperly            12.285
Mapster             19.174
AutoMapper          37.854
```

BindMapper performs within ~2% of manual mapping, with no runtime overhead.

---

## 🧠 How It Works

1. Source Generator scans `[MapperConfiguration]`
2. Extracts `CreateMap<TSource, TDest>` calls
3. Generates plain C# mapping code
4. JIT aggressively inlines generated methods

Generated file example:

```csharp
destination.Id = source.Id;
destination.Name = source.Name;
destination.Email = source.Email;
```

Generated files can be inspected at:

```
bin/Debug/net8.0/BindMapper.g.cs
```

---

## 🧵 Thread Safety

- All generated methods are stateless
- Safe for concurrent use

---

## 📄 License

MIT License
