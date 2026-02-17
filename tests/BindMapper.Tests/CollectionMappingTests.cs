using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;

namespace BindMapper.Tests;

/// <summary>
/// Tests for collection mapping utilities.
/// </summary>
public class CollectionMappingTests
{
    public CollectionMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void MapList_ShouldMapAllItems()
    {
        // Arrange
        var sources = new List<SimpleSource>
        {
            new() { Value = 1, Text = "One" },
            new() { Value = 2, Text = "Two" },
            new() { Value = 3, Text = "Three" }
        };

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveCount(3);
        result[0].Value.Should().Be(1);
        result[0].Text.Should().Be("One");
        result[1].Value.Should().Be(2);
        result[2].Value.Should().Be(3);
    }

    [Fact]
    public void MapList_EmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var sources = new List<SimpleSource>();

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapList_NullList_ShouldReturnEmptyList()
    {
        // Arrange
        List<SimpleSource>? sources = null;

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapArray_ShouldMapAllItems()
    {
        // Arrange
        var sources = new[]
        {
            new SimpleSource { Value = 10, Text = "Ten" },
            new SimpleSource { Value = 20, Text = "Twenty" }
        };

        // Act
        var result = Mapper.ToArray<SimpleDestination>(sources);

        // Assert
        result.Should().HaveCount(2);
        result[0].Value.Should().Be(10);
        result[1].Value.Should().Be(20);
    }

    [Fact]
    public void MapArray_EmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        var sources = Array.Empty<SimpleSource>();

        // Act
        var result = Mapper.ToArray<SimpleDestination>(sources);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapArray_NullArray_ShouldReturnEmptyArray()
    {
        // Arrange
        SimpleSource[]? sources = null;

        // Act
        var result = Mapper.ToArray<SimpleDestination>(sources);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapEnumerable_FromList_ShouldMapAllItems()
    {
        // Arrange
        IEnumerable<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Value = 1 },
            new() { Value = 2 }
        };

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void MapEnumerable_FromArray_ShouldMapAllItems()
    {
        // Arrange
        IEnumerable<SimpleSource> sources = new[]
        {
            new SimpleSource { Value = 5 },
            new SimpleSource { Value = 10 }
        };

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveCount(2);
        result[0].Value.Should().Be(5);
        result[1].Value.Should().Be(10);
    }

    [Fact]
    public void MapEnumerable_NullEnumerable_ShouldReturnEmptyList()
    {
        // Arrange
        IEnumerable<SimpleSource>? sources = null;

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapToList_FromCollection_ShouldMapAllItems()
    {
        // Arrange
        ICollection<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Value = 100 },
            new() { Value = 200 },
            new() { Value = 300 }
        };

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(x => x.Should().BeOfType<SimpleDestination>());
    }

    [Fact]
    public void MapToArray_FromCollection_ShouldMapAllItems()
    {
        // Arrange
        ICollection<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Value = 1 },
            new() { Value = 2 }
        };

        // Act
        var result = Mapper.ToArray<SimpleDestination>(sources);

        // Assert
        result.Should().BeOfType<SimpleDestination[]>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void MapList_WithNestedObjects_ShouldMapNestedCorrectly()
    {
        // Arrange
        var persons = new List<Person>
        {
            new()
            {
                Id = 1,
                FirstName = "John",
                Address = new Address { City = "NYC" }
            },
            new()
            {
                Id = 2,
                FirstName = "Jane",
                Address = new Address { City = "LA" }
            }
        };

        // Act
        var result = Mapper.ToList<PersonDto>(persons);

        // Assert
        result.Should().HaveCount(2);
        result[0].Address.Should().NotBeNull();
        result[0].Address!.City.Should().Be("NYC");
        result[1].Address!.City.Should().Be("LA");
    }
}
