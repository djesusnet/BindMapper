using FluentAssertions;
using VelocityMapper.Tests.Models;
using Xunit;

namespace VelocityMapper.Tests;

/// <summary>
/// Tests for mapper configuration functionality.
/// </summary>
public class MapperConfigurationTests
{
    public MapperConfigurationTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void CreateMap_ShouldReturnMapperConfiguration()
    {
        // Act
        var config = Velocity.CreateMap<SimpleSource, SimpleDestination>();

        // Assert
        config.Should().NotBeNull();
        config.Should().BeOfType<MapperConfiguration<SimpleSource, SimpleDestination>>();
    }

    [Fact]
    public void CreateMap_WithAction_ShouldReturnMapperConfiguration()
    {
        // Act
        var config = Velocity.CreateMap<SimpleSource, SimpleDestination>(cfg =>
        {
            // Configuration action
        });

        // Assert
        config.Should().NotBeNull();
    }

    [Fact]
    public void MapperConfiguration_ForMember_ShouldReturnSameInstance()
    {
        // Act
        var config = new MapperConfiguration<Person, PersonDto>();
        var result = config.ForMember(
            dest => dest.FirstName,
            opt => opt.MapFrom(src => src.FirstName));

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void MapperConfiguration_ReverseMap_ShouldReturnSameInstance()
    {
        // Act
        var config = new MapperConfiguration<Person, PersonDto>();
        var result = config.ReverseMap();

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void MapperConfiguration_ChainedCalls_ShouldWork()
    {
        // Act
        var config = new MapperConfiguration<Person, PersonDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.Ignore())
            .ReverseMap();

        // Assert
        config.Should().NotBeNull();
    }
}
