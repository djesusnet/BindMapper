<p align="center">
  <img src="https://raw.githubusercontent.com/djesusnet/VelocityMapper/refs/heads/main/assets/icon.png" alt="VelocityMapper Logo" width="120">
</p>

# VelocityMapper

**O mapper .NET mais rápido. Zero reflection. Zero overhead.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/VelocityMapper.svg)](https://www.nuget.org/packages/VelocityMapper/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VelocityMapper.svg)](https://www.nuget.org/packages/VelocityMapper/)
[![.NET](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)

VelocityMapper usa **Source Generators** para gerar código de mapeamento otimizado em tempo de compilação. API familiar estilo AutoMapper, performance superior.

---

## 📦 Instalação

```bash
dotnet add package VelocityMapper
```

Frameworks suportados: .NET 6, 8, 9, 10

---

## 🚀 Quick Start

### 1. Crie seus models

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

### 2. Configure os mapeamentos

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

### 3. Use o mapper

```csharp
var user = new User { Id = 1, Name = "João", Email = "joao@email.com" };

// ⚡ Criar nova instância
var dto = Mapper.To<UserDto>(user);

// Zero allocation - mapear para objeto existente
var existingDto = new UserDto();
Mapper.To(user, existingDto);
```

---

## 📚 API

### Mapeamento Básico

```csharp
// Nova instância
var dto = Mapper.To<UserDto>(user);

// Para objeto existente (zero allocation)
Mapper.To(user, existingDto);
```

### Mapeamento de Coleções

```csharp
// ⚡ NOVA API - Mais limpa e com Span!
var users = GetUsers(); // List<User>, User[], IEnumerable<User>

// ToList - Auto-otimizado com CollectionsMarshal.AsSpan (.NET 8+)
List<UserDto> dtos = Mapper.ToList<UserDto>(users);

// ToArray - Otimizado com Span zero-copy
UserDto[] array = Mapper.ToArray<UserDto>(users);

// ToEnumerable - Lazy evaluation (deferred execution)
IEnumerable<UserDto> enumerable = Mapper.ToEnumerable<UserDto>(users);
var filtered = enumerable.Where(x => x.Id > 10).ToList();

// ToSpan - TRUE zero allocation (advanced)
Span<UserDto> destination = stackalloc UserDto[100];
Mapper.ToSpan(users.AsSpan(), destination);

// API Legada (ainda suportada, mas ToList/ToArray são mais rápidos)
List<UserDto> dtos2 = CollectionMapper.MapToList(users, Mapper.To<UserDto>);
```

---

## 🔄 Comportamento de Mapeamento

### Propriedades Extras são Ignoradas Automaticamente

O VelocityMapper mapeia baseado nas **propriedades do destino**. Propriedades que existem apenas na origem são automaticamente ignoradas:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }  // Só existe na entidade
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // PasswordHash não existe → ignorado automaticamente!
}

var dto = Mapper.To<UserDto>(user);
// dto terá: Id, Name, Email
// PasswordHash é ignorado silenciosamente ✓
```

| Cenário | Comportamento |
|---------|---------------|
| Propriedade existe em ambos | ✅ Mapeia |
| Propriedade só na origem | ✅ Ignora silenciosamente |
| Propriedade só no destino | ✅ Mantém valor padrão |

### Atributos

```csharp
public class UserDto
{
    public int Id { get; set; }
    
    [MapFrom("FirstName")]  // Mapeia de propriedade com nome diferente
    public string Name { get; set; }
    
    [IgnoreMap]  // Ignora explicitamente (documentação)
    public string CacheKey { get; set; }
}
```

---

## 🏎️ Performance

Benchmark no .NET 10 (Intel Core i5-14600KF):

| Mapper | Tempo | Comparação |
|--------|-------|------------|
| **VelocityMapper** | **12.03 ns** | Mais rápido |
| Manual | 12.22 ns | baseline |
| Mapperly | 12.29 ns | 2% mais lento |
| Mapster | 18.91 ns | 57% mais lento |
| AutoMapper | 32.87 ns | 173% mais lento |

VelocityMapper é mais rápido que código escrito à mão.

---

## 🔧 Como Funciona

O Source Generator analisa seu código em tempo de compilação e gera métodos otimizados:

```csharp
// Você escreve:
Mapper.CreateMap<User, UserDto>();
var dto = Mapper.To<UserDto>(user);

// O gerador cria automaticamente:
public static UserDto To(User source)
{
    return new UserDto
    {
        Id = source.Id,           // Value types primeiro (cache-friendly)
        Age = source.Age,
        Name = source.Name,       // Reference types depois
        Email = source.Email,
        Address = source.Address is { } addr ? To(addr) : null  // Nested mapping
    };
}
```

---

## 📋 Referência Rápida

| Método | Uso | Allocation | Performance |
|--------|-----|------------|-------------|
| `Mapper.To<TDest>(source)` | Nova instância | DTO size | ⚡⚡⚡ 12ns |
| `Mapper.To(source, dest)` | Objeto existente | 0 B | ⚡⚡⚡ Zero alloc |
| `Mapper.ToList<TDest>(enumerable)` | IEnumerable → Lista | List + DTOs | ⚡⚡⚡ Span-optimized |
| `Mapper.ToArray<TDest>(enumerable)` | IEnumerable → Array | Array + DTOs | ⚡⚡⚡ Span zero-copy |
| `Mapper.ToEnumerable<TDest>(enumerable)` | IEnumerable → IEnumerable | Lazy | ⚡⚡⚡ Deferred execution |
| `Mapper.ToSpan(src, dest)` | Span → Span | 0 B | ⚡⚡⚡ TRUE zero alloc |
| `CollectionMapper.*` (legado) | Compatibilidade | List + DTOs | ⚡⚡ Mais lento |

**Nova API**: `ToList`, `ToArray` e `ToEnumerable` detectam automaticamente List/Array e usam fast-path com Span!

---

## 📄 Licença

MIT License - veja [LICENSE](LICENSE)

