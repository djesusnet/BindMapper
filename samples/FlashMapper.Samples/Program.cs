using FlashMapper;

namespace FlashMapper.Samples;

public class Program
{
    public static void Main(string[] args)
    {
        _ = args;
        Console.WriteLine("=== FlashMapper Samples ===\n");

        // Configure mappings once at startup
        MappingConfiguration.Configure();

        // Example 1: Simple mapping
        SimpleMapping();

        // Example 2: Nested object mapping
        NestedObjectMapping();

        // Example 3: Collection mapping
        CollectionMapping();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void SimpleMapping()
    {
        Console.WriteLine("Example 1: Simple Mapping");
        Console.WriteLine("-------------------------");

        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.Now
        };

        // Map using FlashMapper (generated code - zero reflection!)
        var userDto = Mapper.Map(user);  // Returns UserDto automatically!

        Console.WriteLine($"User: {userDto.Name} ({userDto.Email})");
        Console.WriteLine();
    }

    private static void NestedObjectMapping()
    {
        Console.WriteLine("Example 2: Nested Object Mapping");
        Console.WriteLine("---------------------------------");

        var order = new Order
        {
            Id = 100,
            OrderNumber = "ORD-2026-001",
            Total = 299.99m,
            Customer = new Customer
            {
                Id = 1,
                Name = "Jane Smith",
                Email = "jane@example.com",
                ShippingAddress = new Address
                {
                    Street = "456 Oak Avenue",
                    City = "San Francisco",
                    State = "CA",
                    ZipCode = "94102"
                }
            }
        };

        var orderDto = Mapper.Map(order);  // Returns OrderDto automatically!

        Console.WriteLine($"Order: {orderDto.OrderNumber}");
        Console.WriteLine($"Customer: {orderDto.Customer?.Name}");
        Console.WriteLine($"Address: {orderDto.Customer?.ShippingAddress?.City}, {orderDto.Customer?.ShippingAddress?.State}");
        Console.WriteLine();
    }

    private static void CollectionMapping()
    {
        Console.WriteLine("Example 3: Collection Mapping");
        Console.WriteLine("-----------------------------");

        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 1299.99m, InStock = true },
            new Product { Id = 2, Name = "Mouse", Price = 29.99m, InStock = true },
            new Product { Id = 3, Name = "Keyboard", Price = 89.99m, InStock = false }
        };

        // Map collection (using generated code)
        var productDtos = products.Select(p => Mapper.Map(p)).ToList();  // Each returns ProductDto!

        Console.WriteLine($"Products ({productDtos.Count}):");
        foreach (var dto in productDtos)
        {
            Console.WriteLine($"  - {dto.Name}: ${dto.Price} {(dto.InStock ? "✓" : "✗")}");
        }
        Console.WriteLine();
    }
}

// Domain Models
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal Total { get; set; }
    public Customer? Customer { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Address? ShippingAddress { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

// DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal Total { get; set; }
    public CustomerDto? Customer { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public AddressDto? ShippingAddress { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

// Mapping Configuration
public static class MappingConfiguration
{
    [MapperConfiguration]
    public static void Configure()
    {
        // Simple mappings
        Mapper.CreateMap<User, UserDto>();
        
        // Nested mappings
        Mapper.CreateMap<Order, OrderDto>();
        Mapper.CreateMap<Customer, CustomerDto>();
        Mapper.CreateMap<Address, AddressDto>();
        
        // Product mapping with reverse
        Mapper.CreateMap<Product, ProductDto>().ReverseMap();
    }
}
