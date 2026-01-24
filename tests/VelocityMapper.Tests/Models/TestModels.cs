namespace VelocityMapper.Tests.Models;

// Source models
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public Address? Address { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class Order
{
    public int OrderId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public Customer? Customer { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

// Destination models (DTOs)
public class PersonDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public AddressDto? Address { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class OrderDto
{
    public int OrderId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public CustomerDto? Customer { get; set; }
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

// Models with attributes
public class UserWithAttributes
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string SecretPassword { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class UserWithAttributesDto
{
    public int Id { get; set; }
    
    [MapFrom("UserName")]
    public string Login { get; set; } = "";
    
    [IgnoreMap]
    public string SecretPassword { get; set; } = "";
    
    public string DisplayName { get; set; } = "";
}

// Simple flat models
public class SimpleSource
{
    public int Value { get; set; }
    public string Text { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class SimpleDestination
{
    public int Value { get; set; }
    public string Text { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}
