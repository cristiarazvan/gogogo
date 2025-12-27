using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DockerProject.Data;
using DockerProject.Models;

namespace DockerProject.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Favorites
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var favorites = await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.Restaurant)
                .Where(f => f.UserId == userId)
                .ToListAsync();

            return View(favorites);
        }

        // POST: Favorites/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = _userManager.GetUserId(User);

            // Check if product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Products");
            }

            // Check if already in favorites (no duplication)
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existingFavorite != null)
            {
                TempData["Info"] = "Product is already in your favorites.";
                return RedirectToAction("Index");
            }

            // Add to favorites
            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product added to favorites!";
            return RedirectToAction("Index");
        }

        // POST: Favorites/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int favoriteId)
        {
            var userId = _userManager.GetUserId(User);
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId);

            if (favorite == null)
            {
                TempData["Error"] = "Favorite not found.";
                return RedirectToAction("Index");
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product removed from favorites.";
            return RedirectToAction("Index");
        }

        // POST: Favorites/MoveToCart
        [HttpPost]
        public async Task<IActionResult> MoveToCart(int favoriteId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);
            var favorite = await _context.Favorites
                .Include(f => f.Product)
                .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == userId);

            if (favorite == null)
            {
                TempData["Error"] = "Favorite not found.";
                return RedirectToAction("Index");
            }

            // Check stock
            if (favorite.Product.Stock < quantity)
            {
                TempData["Error"] = $"Not enough stock available. Only {favorite.Product.Stock} items in stock.";
                return RedirectToAction("Index");
            }

            // Check if product is already in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == favorite.ProductId);

            if (existingCartItem != null)
            {
                // Check if total quantity would exceed stock
                if (favorite.Product.Stock < existingCartItem.Quantity + quantity)
                {
                    TempData["Error"] = $"Not enough stock available. Only {favorite.Product.Stock} items in stock.";
                    return RedirectToAction("Index");
                }

                existingCartItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = favorite.ProductId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product moved to cart!";
            return RedirectToAction("Index", "Cart");
        }

        // POST: Favorites/Toggle (for adding/removing from product pages)
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existingFavorite != null)
            {
                // Remove from favorites
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product removed from favorites.";
            }
            else
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Products");
                }

                // Add to favorites
                var favorite = new Favorite
                {
                    UserId = userId,
                    ProductId = productId
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product added to favorites!";
            }

            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}
