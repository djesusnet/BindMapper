<p align="center">
  <img src="https://raw.githubusercontent.com/djesusnet/VelocityMapper/refs/heads/main/assets/icon.png" alt="VelocityMapper Logo" width="120">
</p>

# VelocityMapper

**The fastest .NET mapper. Zero reflection. Zero overhead.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/VelocityMapper.svg)](https://www.nuget.org/packages/VelocityMapper/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VelocityMapper.svg)](https://www.nuget.org/packages/VelocityMapper/)
[![.NET](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)

VelocityMapper uses **Source Generators** to generate optimized mapping code at compile-time. Familiar AutoMapper-style API with superior performance.

---

## 📦 Installation

```bash
dotnet add package VelocityMapper
```

Supported frameworks: .NET 6, 8, 9, 10

---

## 🚀 Quick Start

### 1. Create your models

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Address Address { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public AddressDto Address { get; set; }
}
```

### 2. Configure mappings

```csharp
using VelocityMapper;

public static class MappingConfig
{
    [MapperConfiguration]
    public static void Configure()
    {
        Mapper.CreateMap<User, UserDto>();
        Mapper.CreateMap<Address, AddressDto>();
    }
}
```

### 3. Use the mapper

```csharp
var user = new User { Id = 1, Name = "John", Email = "john@email.com" };

// ⚡ Create new instance
var dto = Mapper.To<UserDto>(user);

// Zero allocation - map to existing object
var existingDto = new UserDto();
Mapper.To(user, existingDto);
```

---

## 📚 API

### Basic Mapping

```csharp
// New instance
var dto = Mapper.To<UserDto>(user);

// To existing object (zero allocation)
Mapper.To(user, existingDto);
```

### Collection Mapping

```csharp
// ⚡ NEW API - Cleaner and faster with Span!
var users = GetUsers(); // List<User>, User[], IEnumerable<User>

// ToList - Auto-optimized with CollectionsMarshal.AsSpan (.NET 8+)
List<UserDto> dtos = Mapper.ToList<UserDto>(users);

// ToArray - Optimized with Span zero-copy
UserDto[] array = Mapper.ToArray<UserDto>(users);

// ToEnumerable - Lazy evaluation (deferred execution)
IEnumerable<UserDto> enumerable = Mapper.ToEnumerable<UserDto>(users);
var filtered = enumerable.Where(x => x.Id > 10).ToList();

// ToSpan - TRUE zero allocation (advanced)
Span<UserDto> destination = stackalloc UserDto[100];
Mapper.ToSpan(users.AsSpan(), destination);

// Legacy API (still supported, but ToList/ToArray are faster)
List<UserDto> dtos2 = CollectionMapper.MapToList(users, Mapper.To<UserDto>);
```

---

## 🔄 Mapping Behavior

### Extra Properties are Automatically Ignored

VelocityMapper maps based on **destination properties**. Properties that exist only in the source are automatically ignored:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }  // Only exists in entity
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // PasswordHash doesn't exist → ignored automatically!
}

var dto = Mapper.To<UserDto>(user);
// dto will have: Id, Name, Email
// PasswordHash is silently ignored ✓
```

| Scenario | Behavior |
|----------|----------|
| Property exists in both | ✅ Maps |
| Property only in source | ✅ Silently ignores |
| Property only in destination | ✅ Keeps default value |

### Attributes

```csharp
public class UserDto
{
    public int Id { get; set; }
    
    [MapFrom("FirstName")]  // Map from differently named property
    public string Name { get; set; }
    
    [IgnoreMap]  // Explicitly ignore (documentation)
    public string CacheKey { get; set; }
}
```

---

## 🏎️ Performance

Benchmark on .NET 10 (Intel Core i5-14600KF):

| Mapper | Time | Comparison |
|--------|------|-----------|
| **VelocityMapper** | **12.03 ns** | Fastest |
| Manual | 12.22 ns | baseline |
| Mapperly | 12.29 ns | 2% slower |
| Mapster | 18.91 ns | 57% slower |
| AutoMapper | 32.87 ns | 173% slower |

VelocityMapper is faster than hand-written code.

---

## 🔧 How It Works

The Source Generator analyzes your code at compile-time and generates optimized methods:

```csharp
// You write:
Mapper.CreateMap<User, UserDto>();
var dto = Mapper.To<UserDto>(user);

// The generator automatically creates:
public static UserDto To(User source)
{
    return new UserDto
    {
        Id = source.Id,           // Value types first (cache-friendly)
        Age = source.Age,
        Name = source.Name,       // Reference types after
        Email = source.Email,
        Address = source.Address is { } addr ? To(addr) : null  // Nested mapping
    };
}
```

---

## 📋 Quick Reference

| Method | Usage | Allocation | Performance |
|--------|-------|-----------|-------------|
| `Mapper.To<TDest>(source)` | New instance | DTO size | ⚡⚡⚡ 12ns |
| `Mapper.To(source, dest)` | Existing object | 0 B | ⚡⚡⚡ Zero alloc |
| `Mapper.ToList<TDest>(enumerable)` | IEnumerable → List | List + DTOs | ⚡⚡⚡ Span-optimized |
| `Mapper.ToArray<TDest>(enumerable)` | IEnumerable → Array | Array + DTOs | ⚡⚡⚡ Span zero-copy |
| `Mapper.ToEnumerable<TDest>(enumerable)` | IEnumerable → IEnumerable | Lazy | ⚡⚡⚡ Deferred execution |
| `Mapper.ToSpan(src, dest)` | Span → Span | 0 B | ⚡⚡⚡ TRUE zero alloc |
| `CollectionMapper.*` (legacy) | Compatibility | List + DTOs | ⚡⚡ Slower |

**New API**: `ToList`, `ToArray` and `ToEnumerable` automatically detect List/Array and use fast-path with Span!

---

## 📄 License

MIT License - see [LICENSE](LICENSE)

