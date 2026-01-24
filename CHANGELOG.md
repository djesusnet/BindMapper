# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-01-24

### Changed

- **Breaking Change**: Renamed `Mapper` class to `Velocity` for a cleaner and more concise API.

#### Migration Guide

Replace all usages of `Mapper` with `Velocity`:

```csharp
// Before (v1.0.0)
Mapper.CreateMap<User, UserDto>();
var dto = Mapper.Map<UserDto>(user);
Mapper.Map(user, existingDto);

// After (v1.1.0)
Velocity.CreateMap<User, UserDto>();
var dto = Velocity.Map<UserDto>(user);
Velocity.Map(user, existingDto);
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
- Collection mapping (`MapList`, `MapArray`, `MapSpan`, `MapSpanTo`)
- Nested object mapping
- Map to existing object (zero allocation)
