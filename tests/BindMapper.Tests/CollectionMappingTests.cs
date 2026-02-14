using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;
using AutoFixture;

namespace BindMapper.Tests;

/// <summary>
/// Tests for collection mapping utilities.
/// </summary>
public class CollectionMappingTests
{
    private readonly Fixture _fixture = new Fixture();
    public CollectionMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void MapList_ShouldMapAllItems()
    {
        // Arrange
        var sources = _fixture.CreateMany<SimpleSource>(3).ToList();

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveSameCount(sources);
        result.Should().BeEquivalentTo(sources, options => options.WithStrictOrdering());
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
        var sources = _fixture.CreateMany<SimpleSource>(2).ToArray();

        // Act
        var result = Mapper.ToArray<SimpleDestination>(sources);

        // Assert
        result.Should().HaveSameCount(sources);
        result.Should().BeEquivalentTo(sources, options => options.WithStrictOrdering());
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
        IEnumerable<SimpleSource> sources = _fixture.CreateMany<SimpleSource>(2).ToList();

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveSameCount(sources);
        result.Should().BeEquivalentTo(sources,options => options.WithStrictOrdering());
    }

    [Fact]
    public void MapEnumerable_FromArray_ShouldMapAllItems()
    {
        // Arrange
        IEnumerable<SimpleSource> sources = _fixture.CreateMany<SimpleSource>(2).ToList();

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveSameCount(sources);
        result.Should().BeEquivalentTo(sources, options => options.WithStrictOrdering());
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
        ICollection<SimpleSource> sources = _fixture.CreateMany<SimpleSource>(3).ToList();

        // Act
        var result = Mapper.ToList<SimpleDestination>(sources);

        // Assert
        result.Should().HaveSameCount(sources);
        result.Should().AllSatisfy(x => x.Should().BeOfType<SimpleDestination>());
    }

    [Fact]
    public void MapToArray_FromCollection_ShouldMapAllItems()
    {
        // Arrange
        ICollection<SimpleSource> sources = _fixture.CreateMany<SimpleSource>(3).ToList();

        // Act
        var result = Mapper.ToArray<SimpleDestination>(sources);

        // Assert
        result.Should().BeOfType<SimpleDestination[]>();
        result.Should().HaveSameCount(sources);
    }

    [Fact]
    public void MapList_WithNestedObjects_ShouldMapNestedCorrectly()
    {
        // Arrange
        var persons = _fixture.CreateMany<Person>(2).ToList();

        // Act
        var result = Mapper.ToList<PersonDto>(persons);

        // Assert
        result.Should().HaveSameCount(persons);
        result.Should().BeEquivalentTo(persons,options => options.WithStrictOrdering());
    }
}
