using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Mapster;
using Riok.Mapperly.Abstractions;
using MapperlyMapper = Riok.Mapperly.Abstractions.MapperAttribute;

namespace VelocityMapper.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        _ = args;
        BenchmarkRunner.Run<MapperBenchmarks>();
    }
}

/// <summary>
/// Benchmarks comparing VelocityMapper with AutoMapper and manual mapping.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class MapperBenchmarks
{
    private Person _person = null!;
    private AutoMapper.IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;
    private PersonMapper _mapperly = null!;
    private PersonDto _personDtoReuseFlash = null!;
    private PersonDto _personDtoReuseAuto = null!;
    private PersonDto _personDtoReuseManual = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup VelocityMapper
        MapperConfig.Configure();

        // Setup AutoMapper
        var autoMapperConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Person, PersonDto>();
            cfg.CreateMap<Address, AddressDto>();
        });
        _autoMapper = autoMapperConfig.CreateMapper();

        // Setup Mapster
        _mapsterConfig = new TypeAdapterConfig();
        _mapsterConfig.NewConfig<Person, PersonDto>();
        _mapsterConfig.NewConfig<Address, AddressDto>();
        _mapsterConfig.Compile();

        // Setup Mapperly
        _mapperly = new PersonMapper();

        // Create test data
        _person = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Age = 30,
            IsActive = true,
            Address = new Address
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        _personDtoReuseFlash = new PersonDto { Address = new AddressDto() };
        _personDtoReuseAuto = new PersonDto { Address = new AddressDto() };
        _personDtoReuseManual = new PersonDto { Address = new AddressDto() };
    }

    [Benchmark(Baseline = true)]
    public PersonDto ManualMapping()
    {
        return new PersonDto
        {
            Id = _person.Id,
            FirstName = _person.FirstName,
            LastName = _person.LastName,
            Email = _person.Email,
            Age = _person.Age,
            IsActive = _person.IsActive,
            Address = _person.Address is not null ? new AddressDto
            {
                Street = _person.Address.Street,
                City = _person.Address.City,
                State = _person.Address.State,
                ZipCode = _person.Address.ZipCode,
                Country = _person.Address.Country
            } : null
        };
    }

    [Benchmark]
    public PersonDto VelocityMapper_Map()
    {
        return Mapper.To(_person);
    }

    [Benchmark]
    public PersonDto AutoMapper_Map()
    {
        return _autoMapper.Map<PersonDto>(_person);
    }

    [Benchmark]
    public PersonDto Mapster_Map()
    {
        return _person.Adapt<PersonDto>(_mapsterConfig);
    }

    [Benchmark]
    public PersonDto Mapperly_Map()
    {
        return _mapperly.Map(_person);
    }

    [Benchmark]
    public PersonDto VelocityMapper_MapToExisting()
    {
        Mapper.To(_person, _personDtoReuseFlash);
        return _personDtoReuseFlash;
    }

    [Benchmark]
    public PersonDto AutoMapper_MapToExisting()
    {
        _autoMapper.Map(_person, _personDtoReuseAuto);
        return _personDtoReuseAuto;
    }

    [Benchmark]
    public PersonDto ManualMapping_ToExisting()
    {
        _personDtoReuseManual.Id = _person.Id;
        _personDtoReuseManual.FirstName = _person.FirstName;
        _personDtoReuseManual.LastName = _person.LastName;
        _personDtoReuseManual.Email = _person.Email;
        _personDtoReuseManual.Age = _person.Age;
        _personDtoReuseManual.IsActive = _person.IsActive;

        if (_person.Address is null)
        {
            _personDtoReuseManual.Address = null;
        }
        else
        {
            _personDtoReuseManual.Address ??= new AddressDto();
            _personDtoReuseManual.Address.Street = _person.Address.Street;
            _personDtoReuseManual.Address.City = _person.Address.City;
            _personDtoReuseManual.Address.State = _person.Address.State;
            _personDtoReuseManual.Address.ZipCode = _person.Address.ZipCode;
            _personDtoReuseManual.Address.Country = _person.Address.Country;
        }

        return _personDtoReuseManual;
    }
}

// Test models
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public Address? Address { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class PersonDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public AddressDto? Address { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}

// VelocityMapper configuration
public static class MapperConfig
{
    [MapperConfiguration]
    public static void Configure()
    {
        Mapper.CreateMap<Person, PersonDto>();
        Mapper.CreateMap<Address, AddressDto>();
    }
}

[MapperlyMapper]
public partial class PersonMapper
{
    public partial PersonDto Map(Person source);
    public partial AddressDto Map(Address source);
}

