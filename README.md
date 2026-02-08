# BindMapper

**BindMapper** is a high-performance object-to-object mapper for .NET, powered by **Source Generators**.  
It generates optimized mapping code at compile-time, eliminating reflection overhead and delivering performance close to hand-written code.

The API is intentionally designed with **AutoMapper-like syntax**, while providing **compile-time safety, predictability, and speed**.

---

## 🚀 Features

- Compile-time mapping via Source Generators
- AutoMapper-inspired configuration (`CreateMap`, `ForMember`, `ReverseMap`)
- Zero reflection
- Allocation-aware APIs
- High-performance collection mapping
- Deterministic and JIT-friendly generated code

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

---

## 🧩 Configuration (AutoMapper-like)

```csharp
using BindMapper;

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

var dto = Mapper.To<UserDto>(user);

var existingDto = new UserDto();
Mapper.To(user, existingDto); // zero allocation
```

---

## 📚 Collection APIs

```csharp
var list = Mapper.ToList<UserDto>(users);
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
