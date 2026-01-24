namespace VelocityMapper;

/// <summary>
/// Marker attribute to indicate that a mapping configuration should be generated.
/// Place this on a static method that calls CreateMap methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MapperConfigurationAttribute : Attribute
{
}

