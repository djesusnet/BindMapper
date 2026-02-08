<p align="center">
  <img src="https://raw.githubusercontent.com/djesusnet/BindMapper/main/assets/icon.png" alt="BindMapper Logo" width="200"/>
</p>


# BindMapper

**BindMapper** is a high-performance object-to-object mapper for .NET, powered by **Source Generators**.  
It generates optimized mapping code at compile-time, eliminating reflection overhead and delivering performance comparable to hand-written code.

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

```csharp
using BindMapper;

public class MappingConfiguration
{
    [MapperConfiguration]
    public static void ConfigureMappings()
    {
        MapperSetup.CreateMap<User, UserDto>();
    }
}
```

### Step 3: Use at Runtime

```csharp
var user = new User { Id = 1, Name = "John", Email = "john@email.com" };

var dto = Mapper.To<UserDto>(user);

var existingDto = new UserDto();
Mapper.To(user, existingDto); // zero allocation
```

---

## 📚 Complete API Reference

### Single Object Mapping

```csharp
var dto = Mapper.To<UserDto>(user);

Mapper.To(user, existingDto); // zero allocation
```

---

### Collection Mapping

```csharp
var list = Mapper.ToList<UserDto>(users);
var array = Mapper.ToArray<UserDto>(users);
var enumerable = Mapper.ToEnumerable<UserDto>(users);
var collection = Mapper.ToCollection<UserDto>(users);
```

---

### Span Mapping (Advanced)

```csharp
ReadOnlySpan<User> source = usersArray;
Span<UserDto> destination = stackalloc UserDto[source.Length];

Mapper.ToSpan(source, destination);
```

⚠️ Destination span must be at least the same length as source.

---

## 📐 Property Mapping Rules

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

## 🏎️ Benchmarks (.NET 10 – Intel i5-14600KF)

```
Mapper              Mean (ns)
--------------------------------
Manual mapping      11.750
BindMapper          12.030
Mapster             19.174
AutoMapper          37.854
```

BindMapper runs within ~2% of hand-written mapping while eliminating runtime overhead.

---

## 🧠 How It Works

- Source Generator scans `[MapperConfiguration]`
- Extracts `CreateMap<TSource, TDest>` calls
- Generates plain C# mapping code
- JIT aggressively inlines generated methods

Generated files:

```
bin/Debug/net8.0/BindMapper.g.cs
```

---

## 🧵 Thread Safety

- All generated methods are stateless
- Fully thread-safe

---

## 📄 License

MIT License
