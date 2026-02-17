using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;

namespace BindMapper.Tests;

/// <summary>
/// Tests for basic object-to-object mapping functionality.
/// </summary>
public class BasicMappingTests
{
    public BasicMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void Map_SimpleObject_ShouldMapAllProperties()
    {
        // Arrange
        var source = new SimpleSource
        {
            Value = 42,
            Text = "Hello World",
            Date = new DateTime(2026, 1, 24),
            Amount = 99.99m
        };

        // Act
        var result = Mapper.To<SimpleDestination>(source);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(42);
        result.Text.Should().Be("Hello World");
        result.Date.Should().Be(new DateTime(2026, 1, 24));
        result.Amount.Should().Be(99.99m);
    }

    [Fact]
    public void Map_Person_ShouldMapAllProperties()
    {
        // Arrange
        var person = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Age = 30,
            IsActive = true
        };

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john@example.com");
        result.Age.Should().Be(30);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Map_PersonWithNullAddress_ShouldMapWithNullAddress()
    {
        // Arrange
        var person = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Address = null
        };

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
        var source = new SimpleSource
        {
            Value = 100,
            Text = "Updated",
            Date = new DateTime(2026, 6, 15),
            Amount = 250.50m
        };

        var existing = new SimpleDestination
        {
            Value = 0,
            Text = "Original",
            Date = DateTime.MinValue,
            Amount = 0m
        };

        // Act
        Mapper.To(source, existing);

        // Assert
        existing.Value.Should().Be(100);
        existing.Text.Should().Be("Updated");
        existing.Date.Should().Be(new DateTime(2026, 6, 15));
        existing.Amount.Should().Be(250.50m);
    }

    [Fact]
    public void Map_GenericMethod_ShouldReturnCorrectType()
    {
        // Arrange
        var person = new Person
        {
            Id = 5,
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().BeOfType<PersonDto>();
        result.Id.Should().Be(5);
        result.FirstName.Should().Be("Jane");
    }
}
