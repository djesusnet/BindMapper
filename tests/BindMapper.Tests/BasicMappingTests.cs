using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;
using AutoFixture;

namespace BindMapper.Tests;

/// <summary>
/// Tests for basic object-to-object mapping functionality.
/// </summary>
public class BasicMappingTests
{
    private readonly Fixture _fixture = new Fixture();
    public BasicMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void Map_SimpleObject_ShouldMapAllProperties()
    {
        // Arrange
        var source = _fixture.Create<SimpleSource>(); 

        // Act
        var result = Mapper.To<SimpleDestination>(source);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(source.Value);
        result.Text.Should().Be(source.Text);
        result.Date.Should().Be(source.Date);
        result.Amount.Should().Be(source.Amount);
    }

    [Fact]
    public void Map_Person_ShouldMapAllProperties()
    {
        // Arrange
        var person = _fixture.Build<Person>()
            .With(x => x.IsActive, true)
            .Create();
                        

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(person.Id);
        result.FirstName.Should().Be(person.FirstName);
        result.LastName.Should().Be(person.LastName);
        result.Email.Should().Be(person.Email);
        result.Age.Should().Be(person.Age);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Map_PersonWithNullAddress_ShouldMapWithNullAddress()
    {
        // Arrange
        var person = _fixture.Build<Person>()
            .With(x => x.Address, (Address?)null)
            .Create();

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().BeNull();
    }

    [Fact]
    public void Map_ToExistingObject_ShouldUpdateAllProperties()
    {
        // Arrange
        var source = _fixture
            .Build<SimpleSource>()
            .With(x=> x.Text, "Updated")        
            .Create();        

        var existing = _fixture
            .Build<SimpleDestination>()
            .With(x=> x.Text, "Original")
            .With(x=> x.Date, DateTime.MinValue)
            .With(x=> x.Amount, 0)
            .Create();   

        // Act
        Mapper.To(source, existing);

        // Assert
        existing.Value.Should().Be(source.Value);
        existing.Text.Should().Be(source.Text);
        existing.Date.Should().Be(source.Date);
        existing.Amount.Should().Be(source.Amount);
    }

    [Fact]
    public void Map_GenericMethod_ShouldReturnCorrectType()
    {
        // Arrange
        var person = _fixture.Build<Person>().
            With(x => x.FirstName, "Jane")
            .Create();

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().BeOfType<PersonDto>();
        result.Id.Should().Be(person.Id);
        result.FirstName.Should().Be("Jane");
    }
}
