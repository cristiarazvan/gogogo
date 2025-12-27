using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DockerProject.Data;
using DockerProject.Models;

namespace DockerProject.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Restaurant)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            ViewBag.TotalPrice = cartItems.Sum(c => c.Product.Price * c.Quantity);
            return View(cartItems);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            // Check if product exists and has stock
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Products");
            }

            if (product.Stock < quantity)
            {
                TempData["Error"] = "Not enough stock available.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            // Check if product is already in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingCartItem != null)
            {
                // Check if total quantity would exceed stock
                if (product.Stock < existingCartItem.Quantity + quantity)
                {
                    TempData["Error"] = "Not enough stock available.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                existingCartItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product added to cart!";
            return RedirectToAction("Index");
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Cart item not found.";
                return RedirectToAction("Index");
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
                TempData["Success"] = "Product removed from cart.";
            }
            else
            {
                // Check stock
                if (cartItem.Product.Stock < quantity)
                {
                    TempData["Error"] = $"Only {cartItem.Product.Stock} items available in stock.";
                    return RedirectToAction("Index");
                }

                cartItem.Quantity = quantity;
                TempData["Success"] = "Quantity updated.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST: Cart/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Cart item not found.";
                return RedirectToAction("Index");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Product removed from cart.";
            return RedirectToAction("Index");
        }

        // POST: Cart/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Validate stock for all items
            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product.Stock < cartItem.Quantity)
                {
                    TempData["Error"] = $"Not enough stock for {cartItem.Product.Title}. Only {cartItem.Product.Stock} available.";
                    return RedirectToAction("Index");
                }
            }

            // Create order
            var order = new Order
            {
                UserId = userId,
                Date = DateTime.Now,
                Items = new List<OrderItem>()
            };

            // Create order items and decrease stock
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price
                };
                order.Items.Add(orderItem);

                // Decrease stock
                cartItem.Product.Stock -= cartItem.Quantity;
            }

            _context.Orders.Add(order);

            // Clear cart
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("OrderSuccess", new { orderId = order.Id });
        }

        // GET: Cart/OrderSuccess
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.TotalPrice = order.Items.Sum(i => i.Price * i.Quantity);
            return View(order);
        }

        // POST: Cart/Clear
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cart cleared.";
            return RedirectToAction("Index");
        }
    }
}
