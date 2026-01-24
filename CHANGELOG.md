# Changelog

All notable changes to this project will be documented in this file.

## [1.2.0] - 2026-01-24

### Changed

- **Breaking Change**: Renamed `VelocityMap` class to `Velocity` for a cleaner and more concise API.

#### Migration Guide

Replace all usages of `VelocityMap` with `Velocity`:

```csharp
// Before (v1.1.0)
VelocityMap.CreateMap<User, UserDto>();
var dto = VelocityMap.Map<UserDto>(user);
VelocityMap.Map(user, existingDto);

// After (v1.2.0)
Velocity.CreateMap<User, UserDto>();
var dto = Velocity.Map<UserDto>(user);
Velocity.Map(user, existingDto);
```

---

## [1.1.0] - 2026-01-24

### Changed

- **Breaking Change**: Renamed `Mapper` class to `VelocityMap` for better brand identity and to avoid conflicts with other mapping libraries.

### Added

- .NET 10 support in CI/CD pipeline

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
