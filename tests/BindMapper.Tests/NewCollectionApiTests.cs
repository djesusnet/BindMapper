using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;

namespace BindMapper.Tests;

/// <summary>
/// Tests for the new high-performance collection API: ToList and ToArray
/// </summary>
public class NewCollectionApiTests
{
    public NewCollectionApiTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void ToList_WithListInput_ShouldMapAllItemsUsingSpan()
    {
        // Arrange
        var users = new List<Person>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Age = 30 },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Age = 25 },
            new() { Id = 3, FirstName = "Bob", LastName = "Johnson", Age = 35 }
        };

        // Act - Nova API mais limpa!
        var dtos = Mapper.ToList<PersonDto>(users);

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].FirstName.Should().Be("John");
        dtos[1].FirstName.Should().Be("Jane");
        dtos[2].FirstName.Should().Be("Bob");
    }

    [Fact]
    public void ToArray_WithArrayInput_ShouldMapAllItemsUsingSpan()
    {
        // Arrange
        var users = new[]
        {
            new Person { Id = 1, FirstName = "Alice", LastName = "Wonder", Age = 28 },
            new Person { Id = 2, FirstName = "Charlie", LastName = "Brown", Age = 32 }
        };

        // Act - API super limpa e performática!
        var dtos = Mapper.ToArray<PersonDto>(users);

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].FirstName.Should().Be("Alice");
        dtos[1].FirstName.Should().Be("Charlie");
    }

    [Fact]
    public void ToList_WithIEnumerable_ShouldMapAllItems()
    {
        // Arrange
        IEnumerable<Person> users = GetUsersEnumerable();

        // Act
        var dtos = Mapper.ToList<PersonDto>(users);

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].FirstName.Should().Be("Mike");
        dtos[1].FirstName.Should().Be("Sarah");
    }

    [Fact]
    public void ToArray_WithEmptyList_ShouldReturnEmptyArray()
    {
        // Arrange
        var emptyList = new List<Person>();

        // Act
        var result = Mapper.ToArray<PersonDto>(emptyList);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToList_WithNull_ShouldReturnEmptyList()
    {
        // Arrange
        List<Person>? nullList = null;

        // Act
        var result = Mapper.ToList<PersonDto>(nullList);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToList_WithLargeCollection_ShouldBePerformant()
    {
        // Arrange
        var largeList = Enumerable.Range(1, 10000)
            .Select(i => new Person 
            { 
                Id = i, 
                FirstName = $"User{i}", 
                LastName = $"Last{i}",
                Age = 20 + (i % 50)
            })
            .ToList();

        // Act
        var dtos = Mapper.ToList<PersonDto>(largeList);

        // Assert
        dtos.Should().HaveCount(10000);
        dtos[0].FirstName.Should().Be("User1");
        dtos[9999].FirstName.Should().Be("User10000");
    }

    [Fact]
    public void ToSpan_ShouldMapWithZeroAllocation()
    {
        // Arrange
        var source = new[]
        {
            new Person { Id = 1, FirstName = "Zero", LastName = "Alloc", Age = 30 },
            new Person { Id = 2, FirstName = "Max", LastName = "Perf", Age = 25 }
        };
        var destination = new PersonDto[2];

        // Act - TRUE zero allocation!
        Mapper.ToSpan(source.AsSpan(), destination.AsSpan());

        // Assert
        destination[0].FirstName.Should().Be("Zero");
        destination[1].FirstName.Should().Be("Max");
    }

    [Fact]
    public void ToEnumerable_ShouldMapWithLazyEvaluation()
    {
        // Arrange
        var users = new List<Person>
        {
            new() { Id = 1, FirstName = "Lazy", LastName = "Load", Age = 30 },
            new() { Id = 2, FirstName = "Deferred", LastName = "Exec", Age = 25 }
        };

        // Act - Lazy evaluation, não executa imediatamente
        IEnumerable<PersonDto> enumerable = Mapper.ToEnumerable<PersonDto>(users);

        // Assert - Só materializa quando iterado
        var list = enumerable.ToList();
        list.Should().HaveCount(2);
        list[0].FirstName.Should().Be("Lazy");
        list[1].FirstName.Should().Be("Deferred");
    }

    [Fact]
    public void ToEnumerable_WithWhere_ShouldSupportLinq()
    {
        // Arrange
        var users = new List<Person>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Age = 30 },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Age = 25 },
            new() { Id = 3, FirstName = "Bob", LastName = "Johnson", Age = 35 }
        };

        // Act - ToEnumerable suporta LINQ!
        var result = Mapper.ToEnumerable<PersonDto>(users)
            .Where(dto => dto.Age > 28)
            .ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("John");
        result[1].FirstName.Should().Be("Bob");
    }

    private static IEnumerable<Person> GetUsersEnumerable()
    {
        yield return new Person { Id = 1, FirstName = "Mike", LastName = "Test", Age = 40 };
        yield return new Person { Id = 2, FirstName = "Sarah", LastName = "Demo", Age = 35 };
    }
}
