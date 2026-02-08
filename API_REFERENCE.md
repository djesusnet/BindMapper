# 📚 VelocityMapper - Referência Completa da API

## 🎯 API Atual (v1.1.0+)

### Configuração

```csharp
[MapperConfiguration]
public static void Configure()
{
    Mapper.CreateMap<User, UserDto>();
    Mapper.CreateMap<Address, AddressDto>();
}
```

---

## 1️⃣ Mapeamento de Objeto Único

### `To<TDestination>(source)` - Nova instância

```csharp
User user = GetUser();
UserDto dto = Mapper.To<UserDto>(user);
```

**Performance:** 12.03 ns  
**Allocation:** Tamanho do DTO  
**Uso:** Criação de nova instância

### `To(source, destination)` - Objeto existente

```csharp
User user = GetUser();
UserDto existingDto = GetCachedDto();
Mapper.To(user, existingDto);
```

**Performance:** ~10 ns  
**Allocation:** 0 B (zero allocation!)  
**Uso:** Atualizar objeto existente, reutilização de buffers

---

## 2️⃣ Mapeamento de Coleções

### `ToList<TDestination>(enumerable)` - Materializa em List

```csharp
List<User> users = GetUsers();
List<UserDto> dtos = Mapper.ToList<UserDto>(users);
```

**Fast-paths automáticos:**
- `List<T>` → Usa `CollectionsMarshal.AsSpan()` (.NET 8+)
- `Array` → Usa `AsSpan()` para zero-copy
- `IEnumerable` → Fallback com foreach

**Performance:** ~1.2 μs para 100 items  
**Allocation:** List + DTOs  
**Uso:** Quando precisa de List

### `ToArray<TDestination>(enumerable)` - Materializa em Array

```csharp
User[] users = GetUsers();
UserDto[] dtos = Mapper.ToArray<UserDto>(users);
```

**Fast-paths automáticos:**
- `Array` → Usa `AsSpan()` direto (fastest!)
- `List<T>` → Usa `CollectionsMarshal.AsSpan()` (.NET 8+)
- `IEnumerable` → Materializa para List primeiro

**Performance:** ~1.3 μs para 100 items  
**Allocation:** Array + DTOs  
**Uso:** Quando a API espera array

### `ToEnumerable<TDestination>(enumerable)` - Lazy evaluation

```csharp
IEnumerable<User> users = GetUsers();
IEnumerable<UserDto> enumerable = Mapper.ToEnumerable<UserDto>(users);

// Composição com LINQ
var filtered = enumerable
    .Where(dto => dto.Age > 18)
    .OrderBy(dto => dto.Name)
    .ToList();  // Só aqui executa o mapeamento!
```

**Performance:** Deferred execution  
**Allocation:** Lazy (só aloca quando materializado)  
**Uso:** LINQ queries, composição, processamento lazy

### `ToSpan(source, destination)` - Zero allocation

```csharp
User[] users = GetUsers();
Span<UserDto> buffer = stackalloc UserDto[users.Length];
Mapper.ToSpan(users.AsSpan(), buffer);

// Ou com ArrayPool
var buffer = ArrayPool<UserDto>.Shared.Rent(users.Length);
try
{
    Mapper.ToSpan(users.AsSpan(), buffer.AsSpan());
    // Use buffer...
}
finally
{
    ArrayPool<UserDto>.Shared.Return(buffer);
}
```

**Performance:** ~1.1 μs para 100 items  
**Allocation:** 0 B (TRUE zero allocation!)  
**Uso:** Hot paths, loops críticos, processamento de alta performance

---

## 📊 Tabela de Decisão

| Preciso de... | Use | Razão |
|---------------|-----|-------|
| Lista | `ToList<T>()` | Fast-path automático, uso geral |
| Array | `ToArray<T>()` | Fast-path para arrays, API compatível |
| LINQ composição | `ToEnumerable<T>()` | Lazy evaluation, defer execution |
| Máxima performance | `ToSpan()` | Zero allocation, stack only |
| Atualizar existente | `To(src, dest)` | Zero allocation, reutiliza objeto |
| Criar novo | `To<T>()` | 12ns, mais rápido que manual |

---

## 🚀 Exemplos Práticos

### API Controller

```csharp
[HttpGet]
public IActionResult GetUsers()
{
    List<User> users = _repository.GetAll();
    
    // Uma linha, Span-optimized!
    return Ok(Mapper.ToList<UserDto>(users));
}
```

### Processamento em Lote (Zero Allocation)

```csharp
const int BatchSize = 1000;
Span<UserDto> batch = stackalloc UserDto[BatchSize];

for (int i = 0; i < users.Length; i += BatchSize)
{
    var sourceBatch = users.AsSpan(i, Math.Min(BatchSize, users.Length - i));
    var destBatch = batch[..sourceBatch.Length];
    
    Mapper.ToSpan(sourceBatch, destBatch);
    
    ProcessBatch(destBatch);  // Zero heap allocation!
}
```

### Query com Filtro (Lazy)

```csharp
var query = Mapper.ToEnumerable<UserDto>(users)
    .Where(dto => dto.IsActive)
    .OrderByDescending(dto => dto.CreatedAt)
    .Take(10);

// Só executa aqui
var topUsers = query.ToList();
```

---

## ⚠️ API Legada (Compatibilidade)

As APIs legadas como `MapToList` e `MapToArray` ainda funcionam:

```csharp
// ✅ API moderna (recomendada)
Mapper.ToList<UserDto>(users);
Mapper.ToArray<UserDto>(users);

// ✅ API legada (ainda suportada)
Mapper.MapToList(users, user => Mapper.To<UserDto>(user));
Mapper.MapToArray(users, user => Mapper.To<UserDto>(user));
```

---

## 🎓 Boas Práticas

### 1. Prefira ToList para uso geral
```csharp
// ✅ BOM
var dtos = Mapper.ToList<UserDto>(users);
```

### 2. Use ToSpan em hot paths
```csharp
// ✅ ÓTIMO para loops críticos
Span<UserDto> buffer = stackalloc UserDto[100];
for (int i = 0; i < iterations; i++)
{
    Mapper.ToSpan(source, buffer);
    Process(buffer);
}
```

### 3. Use ToEnumerable para LINQ
```csharp
// ✅ PERFEITO para composição
var result = Mapper.ToEnumerable<UserDto>(users)
    .Where(x => x.Age > 18)
    .OrderBy(x => x.Name)
    .Take(10)
    .ToList();
```

### 4. Materialize antes do fast-path
```csharp
// ❌ EVITE - IEnumerable é slow
IEnumerable<User> query = GetQuery();
var dtos = Mapper.ToList<UserDto>(query);

// ✅ MELHOR - Materialize primeiro
var users = GetQuery().ToList();
var dtos = Mapper.ToList<UserDto>(users);  // Fast-path!
```

---

## 📈 Benchmarks

```
BenchmarkDotNet v0.13.12, .NET 10

| Method                  | Items | Mean      | Allocated |
|-------------------------|-------|-----------|-----------|
| To (single)             | 1     | 12.03 ns  | 48 B      |
| ToList_List_NET8        | 100   | 1.234 μs  | 3.2 KB    |
| ToArray_Array           | 100   | 1.289 μs  | 3.2 KB    |
| ToEnumerable (deferred) | 100   | -         | 0 B       |
| ToSpan (zero alloc)     | 100   | 1.123 μs  | 0 B       |
| CollectionMapper (old)  | 100   | 1.456 μs  | 3.3 KB    |
```

---

## 🔗 Links

- [README](README.md) - Introdução e Quick Start
- [CHANGELOG](CHANGELOG.md) - Histórico de mudanças
- [COLLECTION_API](COLLECTION_API.md) - Guia detalhado de coleções
- [GitHub](https://github.com/djesusnet/VelocityMapper)
- [NuGet](https://www.nuget.org/packages/VelocityMapper/)
