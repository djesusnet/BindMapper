using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;
using AutoFixture;

namespace BindMapper.Tests;

/// <summary>
/// Tests for nested object mapping functionality.
/// </summary>
public class NestedMappingTests
{
    private readonly Fixture _fixture = new Fixture();
    public NestedMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void Map_PersonWithAddress_ShouldMapNestedObject()
    {
        // Arrange
        var person = _fixture.Create<Person>();       

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be(person.Address!.Street);
        result.Address.City.Should().Be(person.Address!.City);
        result.Address.State.Should().Be(person.Address!.State);
        result.Address.ZipCode.Should().Be(person.Address!.ZipCode);
        result.Address.Country.Should().Be(person.Address!.Country);
    }

    [Fact]
    public void Map_OrderWithCustomer_ShouldMapNestedCustomer()
    {
        // Arrange
        var order = _fixture.Create<Order>();        

        // Act
        var result = Mapper.To<OrderDto>(order);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(order.OrderId);
        result.ProductName.Should().Be(order.ProductName);
        result.Price.Should().Be(order.Price);
        result.Quantity.Should().Be(order.Quantity);
        result.Customer.Should().NotBeNull();
        result.Customer!.CustomerId.Should().Be(order.Customer!.CustomerId);
        result.Customer.Name.Should().Be(order.Customer.Name);
        result.Customer.Email.Should().Be(order.Customer.Email);
    }

    [Fact]
    public void Map_OrderWithNullCustomer_ShouldMapWithNullCustomer()
    {
        // Arrange
        var order = _fixture.Build<Order>()
            .With(x=> x.Customer, (Customer?)null)
            .Create();
        // Act
        var result = Mapper.To<OrderDto>(order);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(order.OrderId);
        result.Customer.Should().BeNull();
    }

    [Fact]
    public void Map_ToExistingWithNestedObject_ShouldUpdateNested()
    {
        var source = _fixture
          .Build<SimpleSource>()
          .With(x => x.Text, "Updated")
          .Create();

        // Arrange
        var person = _fixture.Build<Person>()
            .With(x => x.FirstName, "Updated")
            .Create();

        var existing = _fixture.Build<PersonDto>()
            .With(x => x.FirstName, "Original")
            .Create();

        // Act
        Mapper.To(person, existing);

        // Assert
        existing.FirstName.Should().Be(person.FirstName);
        existing.Address.Should().NotBeNull();
        existing.Address!.Street.Should().Be(person.Address!.Street);
        existing.Address.City.Should().Be(person.Address.City);
    }

    [Fact]
    public void Map_ToExistingWithNullNestedToNonNull_ShouldCreateNested()
    {
        // Arrange
        var person = _fixture.Create<Person>();

        var existing = _fixture.Create<PersonDto>();        

        // Act
        Mapper.To(person, existing);

        // Assert
        existing.Address.Should().NotBeNull();
        existing.Address!.Street.Should().Be(person.Address!.Street);
        existing.Address.City.Should().Be(person.Address.City);
    }
}
