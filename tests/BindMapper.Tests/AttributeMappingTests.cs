using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;

namespace BindMapper.Tests;

/// <summary>
/// Tests for attribute-based mapping functionality.
/// </summary>
public class AttributeMappingTests
{
    public AttributeMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void Map_WithMapFromAttribute_ShouldMapFromDifferentPropertyName()
    {
        // Arrange
        var user = new UserWithAttributes
        {
            Id = 1,
            UserName = "johndoe",
            SecretPassword = "secret123",
            DisplayName = "John Doe"
        };

        // Act
        var result = Mapper.To<UserWithAttributesDto>(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Login.Should().Be("johndoe"); // Mapped from UserName via [MapFrom]
        result.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void Map_WithIgnoreMapAttribute_ShouldNotMapIgnoredProperty()
    {
        // Arrange
        var user = new UserWithAttributes
        {
            Id = 1,
            UserName = "johndoe",
            SecretPassword = "supersecret",
            DisplayName = "John Doe"
        };

        // Act
        var result = Mapper.To<UserWithAttributesDto>(user);

        // Assert
        result.Should().NotBeNull();
        result.SecretPassword.Should().BeEmpty(); // Should not be mapped due to [IgnoreMap]
    }

    [Fact]
    public void Map_ToExistingWithIgnoreAttribute_ShouldPreserveIgnoredProperty()
    {
        // Arrange
        var user = new UserWithAttributes
        {
            Id = 2,
            UserName = "janedoe",
            SecretPassword = "newsecret",
            DisplayName = "Jane Doe"
        };

        var existing = new UserWithAttributesDto
        {
            Id = 0,
            Login = "",
            SecretPassword = "existingsecret", // Should be preserved
            DisplayName = ""
        };

        // Act
        Mapper.To(user, existing);

        // Assert
        existing.Id.Should().Be(2);
        existing.Login.Should().Be("janedoe");
        existing.SecretPassword.Should().Be("existingsecret"); // Preserved, not overwritten
        existing.DisplayName.Should().Be("Jane Doe");
    }
}
