using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DockerProject.Data;
using DockerProject.Services;

namespace DockerProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIChatController : ControllerBase
{
    private readonly AIChatService _chatService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AIChatController> _logger;

    public AIChatController(AIChatService chatService, ApplicationDbContext context, ILogger<AIChatController> logger)
    {
        _chatService = chatService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AIChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new AIChatResponse
            {
                Success = false,
                Message = "Please enter a message."
            });
        }

        // Get product with related data
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Restaurant)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product == null)
        {
            return NotFound(new AIChatResponse
            {
                Success = false,
                Message = "Product not found."
            });
        }

        // Build product context
        var productContext = new ProductContext
        {
            ProductName = product.Title,
            ProductDescription = product.Description,
            Price = product.Price,
            CategoryName = product.Category?.Name ?? "Uncategorized",
            RestaurantName = product.Restaurant?.Name ?? "Unknown Restaurant",
            InStock = product.Stock > 0,
            StockCount = product.Stock,
            Reviews = product.Reviews?.Select(r => r.Text ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList()
        };

        // Get AI response
        var response = await _chatService.GetResponseAsync(request.Message, productContext);

        return Ok(response);
    }
}
