using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AzureCachingForRedis.Controllers;

[ApiController]
[Route("{controller}")]
public class ProductsController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public ProductsController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<Product>> Get(int id, CancellationToken cancellationToken)
    {
        var cachedProduct = await _cache.GetStringAsync(id.ToString(), cancellationToken);
        
        if (cachedProduct != null)
        {
            return Ok(JsonSerializer.Deserialize<Product>(cachedProduct));
        }

        var dbProduct = Product.DbProducts.Where(p => p.Id == id).FirstOrDefault();

        if (dbProduct is null)
        {
            return NotFound();
        }

        var options = new DistributedCacheEntryOptions()
        {
            SlidingExpiration = TimeSpan.FromHours(6),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        };

        await _cache.SetStringAsync(dbProduct.Id.ToString(), JsonSerializer.Serialize(dbProduct), options, cancellationToken);

        return Ok(dbProduct);
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }

    // Simulates the database products.
    public static List<Product> DbProducts = 
        [
            new(){ Id = 1, Name = "T-Shirt", Price = 30 },
            new(){ Id = 2, Name = "Jeans", Price = 70 },
            new(){ Id = 3, Name = "Dress", Price = 95 },
            new(){ Id = 4, Name = "Ball", Price = 6 },
            new(){ Id = 5, Name = "Laptop", Price = 400 },
        ];
}