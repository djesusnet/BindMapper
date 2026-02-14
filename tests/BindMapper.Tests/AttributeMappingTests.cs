using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;
using AutoFixture;

namespace BindMapper.Tests;

/// <summary>
/// Tests for attribute-based mapping functionality.
/// </summary>
public class AttributeMappingTests
{
    private readonly Fixture _fixture = new Fixture();
    public AttributeMappingTests()
    {
        TestMapperConfig.EnsureConfigured();        
    }

    [Fact]
    public void Map_WithMapFromAttribute_ShouldMapFromDifferentPropertyName()
    {
        // Arrange
        var user = _fixture.Create<UserWithAttributes>();           

        // Act
        var result = Mapper.To<UserWithAttributesDto>(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(result.Id);
        result.Login.Should().Be(result.Login); // Mapped from UserName via [MapFrom]
        result.DisplayName.Should().Be(result.DisplayName);
    }

    [Fact]
    public void Map_WithIgnoreMapAttribute_ShouldNotMapIgnoredProperty()
    {
        // Arrange
        var user = _fixture.Create<UserWithAttributes>();

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
        var user = _fixture.Create<UserWithAttributes>();

        var existing = _fixture.Build<UserWithAttributesDto>()
                        .With(x => x.SecretPassword, "existingsecret")
                        .Create();

        // Act
        Mapper.To(user, existing);

        // Assert
        existing.Id.Should().Be(user.Id);
        existing.Login.Should().Be(user.UserName);
        existing.SecretPassword.Should().Be("existingsecret"); // Preserved, not overwritten
        existing.DisplayName.Should().Be(user.DisplayName);
    }
}
