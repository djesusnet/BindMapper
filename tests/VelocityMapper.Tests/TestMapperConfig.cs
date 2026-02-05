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
        Configuration.CreateMap<Person, PersonDto>();
        Configuration.CreateMap<Address, AddressDto>();
        
        // Nested object mappings
        Configuration.CreateMap<Order, OrderDto>();
        Configuration.CreateMap<Customer, CustomerDto>();
        
        // Mapping with attributes
        Configuration.CreateMap<UserWithAttributes, UserWithAttributesDto>();
        
        // Simple mappings
        Configuration.CreateMap<SimpleSource, SimpleDestination>();
    }
}
