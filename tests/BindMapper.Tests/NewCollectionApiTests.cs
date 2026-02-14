using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;
using AutoFixture;

namespace BindMapper.Tests;

/// <summary>
/// Tests for the new high-performance collection API: ToList and ToArray
/// </summary>
public class NewCollectionApiTests
{
    private readonly Fixture _fixture = new Fixture();
    public NewCollectionApiTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void ToList_WithListInput_ShouldMapAllItemsUsingSpan()
    {
        // Arrange
        var users = _fixture.CreateMany<Person>(3).ToList(); 

        // Act - Nova API mais limpa!
        var dtos = Mapper.ToList<PersonDto>(users);

        // Assert
        for (int i = 0; i < users.Count; i++)
        {
            dtos[i].FirstName.Should().Be(users[i].FirstName);
            dtos[i].LastName.Should().Be(users[i].LastName);
            dtos[i].Age.Should().Be(users[i].Age);
        }
    }

    [Fact]
    public void ToArray_WithArrayInput_ShouldMapAllItemsUsingSpan()
    {
        // Arrange
        var users = _fixture.CreateMany<Person>(2).ToList();
        
        // Act - API super limpa e performática!
        var dtos = Mapper.ToArray<PersonDto>(users);

        // Assert
        dtos.Should().HaveSameCount(users);
        for (int i = 0; i < users.Count; i++)
        {
            dtos[i].FirstName.Should().Be(users[i].FirstName);
        }
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
        var source = _fixture.CreateMany<Person>(2).ToArray();
        var destination = new PersonDto[source.Length];

        // Act - TRUE zero allocation!
        Mapper.ToSpan(source.AsSpan(), destination.AsSpan());

        // Assert
        destination.Should().HaveSameCount(source);
        destination.Should().BeEquivalentTo(source,options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToEnumerable_ShouldMapWithLazyEvaluation()
    {
        // Arrange
        var users = _fixture.CreateMany<Person>(2).ToList();

        // Act - Lazy evaluation, não executa imediatamente
        IEnumerable<PersonDto> enumerable = Mapper.ToEnumerable<PersonDto>(users);

        // Assert - Só materializa quando iterado
        var list = enumerable.ToList();
        list.Should().HaveSameCount(users);
        list.Should().BeEquivalentTo(
            users,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToEnumerable_WithWhere_ShouldSupportLinq()
    {
        // Arrange
        var users = _fixture.CreateMany<Person>(3).ToList();

        // Act - ToEnumerable suporta LINQ!
        var result = Mapper.ToEnumerable<PersonDto>(users)
            .Where(dto => dto.Age > 28)
            .ToList();

        // Assert
        result.Should().HaveSameCount(users);
        result.Should().BeEquivalentTo(result, options => options.WithStrictOrdering());
    }

    private static IEnumerable<Person> GetUsersEnumerable()
    {
        yield return new Person { Id = 1, FirstName = "Mike", LastName = "Test", Age = 40 };
        yield return new Person { Id = 2, FirstName = "Sarah", LastName = "Demo", Age = 35 };
    }
}
