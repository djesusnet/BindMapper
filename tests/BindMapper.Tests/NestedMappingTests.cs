using FluentAssertions;
using BindMapper.Tests.Models;
using Xunit;

namespace BindMapper.Tests;

/// <summary>
/// Tests for nested object mapping functionality.
/// </summary>
public class NestedMappingTests
{
    public NestedMappingTests()
    {
        TestMapperConfig.EnsureConfigured();
    }

    [Fact]
    public void Map_PersonWithAddress_ShouldMapNestedObject()
    {
        // Arrange
        var person = new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Age = 30,
            IsActive = true,
            Address = new Address
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        // Act
        var result = Mapper.To<PersonDto>(person);

        // Assert
        result.Should().NotBeNull();
        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be("123 Main St");
        result.Address.City.Should().Be("New York");
        result.Address.State.Should().Be("NY");
        result.Address.ZipCode.Should().Be("10001");
        result.Address.Country.Should().Be("USA");
    }

    [Fact]
    public void Map_OrderWithCustomer_ShouldMapNestedCustomer()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 100,
            ProductName = "Laptop",
            Price = 1299.99m,
            Quantity = 2,
            Customer = new Customer
            {
                CustomerId = 50,
                Name = "Acme Corp",
                Email = "orders@acme.com"
            }
        };

        // Act
        var result = Mapper.To<OrderDto>(order);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(100);
        result.ProductName.Should().Be("Laptop");
        result.Price.Should().Be(1299.99m);
        result.Quantity.Should().Be(2);
        result.Customer.Should().NotBeNull();
        result.Customer!.CustomerId.Should().Be(50);
        result.Customer.Name.Should().Be("Acme Corp");
        result.Customer.Email.Should().Be("orders@acme.com");
    }

    [Fact]
    public void Map_OrderWithNullCustomer_ShouldMapWithNullCustomer()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 101,
            ProductName = "Mouse",
            Price = 29.99m,
            Quantity = 5,
            Customer = null
        };

        // Act
        var result = Mapper.To<OrderDto>(order);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(101);
        result.Customer.Should().BeNull();
    }

    [Fact]
    public void Map_ToExistingWithNestedObject_ShouldUpdateNested()
    {
        // Arrange
        var person = new Person
        {
            Id = 1,
            FirstName = "Updated",
            Address = new Address
            {
                Street = "456 New St",
                City = "Los Angeles"
            }
        };

        var existing = new PersonDto
        {
            Id = 0,
            FirstName = "Original",
            Address = new AddressDto
            {
                Street = "Old St",
                City = "Old City"
            }
        };

        // Act
        Mapper.To(person, existing);

        // Assert
        existing.FirstName.Should().Be("Updated");
        existing.Address.Should().NotBeNull();
        existing.Address!.Street.Should().Be("456 New St");
        existing.Address.City.Should().Be("Los Angeles");
    }

    [Fact]
    public void Map_ToExistingWithNullNestedToNonNull_ShouldCreateNested()
    {
        // Arrange
        var person = new Person
        {
            Id = 1,
            FirstName = "John",
            Address = new Address
            {
                Street = "New Street",
                City = "New City"
            }
        };

        var existing = new PersonDto
        {
            Id = 0,
            FirstName = "Original",
            Address = null
        };

        // Act
        Mapper.To(person, existing);

        // Assert
        existing.Address.Should().NotBeNull();
        existing.Address!.Street.Should().Be("New Street");
        existing.Address.City.Should().Be("New City");
    }
}
