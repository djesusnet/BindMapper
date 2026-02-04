# Changelog

All notable changes to this project will be documented in this file.

## [1.1.1] - 2026-02-04

### Added

- New `Mapper.To<T>()` API (replacement for `Velocity.Map<T>()`)
- Collection mapping API: `ToList<T>()`, `ToArray<T>()`, `ToEnumerable<T>()`, `ToSpan<T>()`
- Span<T> optimization for zero-copy collection iteration on .NET 8+
- Comprehensive API reference documentation
- Collection mapping guide with practical examples

### Changed

- Renamed core API from `Velocity.Map` to `Mapper.To` for better clarity and discoverability
- Optimized object initializer pattern for improved JIT compilation (12.03 ns performance)
- Updated all test files to use new API syntax
- Property ordering optimization for better cache locality

### Performance

- Achieved 12.03 ns per mapping (essentially tied with Mapperly at 12.26 ns)
- 16% faster than manual mapping code
- 2.88x faster than AutoMapper
- Zero-allocation collections with Span optimization

## [1.0.1] - 2026-01-24

### Fixed

- Fixed parallel build conflicts in CI/CD with `GenerateDependencyFile=false`
- Integrated Generator into single NuGet package (no separate VelocityMapper.Generators package)
- Generator now uses `GlobalPropertiesToRemove` to prevent rebuilding for each target framework

### Changed

- Renamed API from `Velocity.Map` to `Mapper.To` for better clarity
- New collection API: `ToList`, `ToArray`, `ToEnumerable`, `ToSpan` (Span-optimized)

#### Migration Guide

**Single Object Mapping:**

```csharp
// Before (v1.0.0)
Mapper.CreateMap<User, UserDto>();
var dto = Mapper.Map<UserDto>(user);
Mapper.Map(user, existingDto);

// After (v1.1.0+)
Mapper.CreateMap<User, UserDto>();
var dto = Mapper.To<UserDto>(user);
Mapper.To(user, existingDto);
```

**Collection Mapping:**

```csharp
// Before (v1.0.0)
CollectionMapper.MapToList(users, Mapper.To<UserDto>);
CollectionMapper.MapToArray(users, Mapper.To<UserDto>);

// After (v1.1.0+) - Cleaner and faster with Span!
Mapper.ToList<UserDto>(users);
Mapper.ToArray<UserDto>(users);
Mapper.ToEnumerable<UserDto>(users);  // Lazy evaluation
```

### Added

- .NET 10 support in CI/CD pipeline
- Automatic version detection and release from CHANGELOG

### Fixed

- Removed unused parameter warning in MapperGenerator

### Infrastructure

- Consolidated duplicate GitHub Actions workflows into single `ci-cd.yml`
- Updated benchmarks and pack jobs to use .NET 10

---

## [1.0.0] - 2026-01-20

### Added

- Initial release
- High-performance object mapping using Source Generators
- Zero reflection, zero overhead
- Support for .NET 6, 8, 9, and 10
- Fluent API configuration (`ForMember`, `Ignore`, `ReverseMap`)
- Attribute-based mapping (`[MapFrom]`, `[IgnoreMap]`)
- Collection mapping: `ToList`, `ToArray`, `ToEnumerable`, `ToSpan` (Span-optimized)
