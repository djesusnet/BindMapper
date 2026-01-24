using VelocityMapper.Tests.Models;

namespace VelocityMapper.Tests;

/// <summary>
/// Mapper configuration for all test scenarios.
/// </summary>
public static class TestMapperConfig
{
    private static bool _isConfigured;
    private static readonly object _lock = new();

    public static void EnsureConfigured()
    {
        if (_isConfigured) return;
        
        lock (_lock)
        {
            if (_isConfigured) return;
            Configure();
            _isConfigured = true;
        }
    }

    [MapperConfiguration]
    public static void Configure()
    {
        // Basic mappings
        Velocity.CreateMap<Person, PersonDto>();
        Velocity.CreateMap<Address, AddressDto>();
        
        // Nested object mappings
        Velocity.CreateMap<Order, OrderDto>();
        Velocity.CreateMap<Customer, CustomerDto>();
        
        // Mapping with attributes
        Velocity.CreateMap<UserWithAttributes, UserWithAttributesDto>();
        
        // Simple mappings
        Velocity.CreateMap<SimpleSource, SimpleDestination>();
    }
}
