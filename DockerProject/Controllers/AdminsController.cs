using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DockerProject.Models;
using DockerProject.Data;

namespace DockerProject.Controllers
{
    [Authorize(Roles = "Admin")] // <--- Doar Adminii au acces aici
    public class AdminsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminsController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Roles = roles
                });
            }

            return View(userList);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to selected role
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Remove user from all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add user to new role
            if (!string.IsNullOrEmpty(newRole))
            {
                await _userManager.AddToRoleAsync(user, newRole);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent admin from deleting themselves
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                TempData["Error"] = "Nu poți șterge propriul cont!";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Utilizatorul {user.Email} a fost șters cu succes.";
            }
            else
            {
                TempData["Error"] = "Eroare la ștergerea utilizatorului.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Notifications - View all pending items
        public async Task<IActionResult> Notifications()
        {
            var viewModel = new NotificationsViewModel();

            // Get pending restaurants
            var pendingRestaurants = await _context.Restaurants
                .Include(r => r.Owner)
                .Where(r => !r.IsApproved)
                .Select(r => new PendingRestaurant
                {
                    Id = r.Id,
                    Name = r.Name,
                    ImagePath = r.ImagePath,
                    OwnerName = r.Owner.FullName,
                    OwnerEmail = r.Owner.Email,
                    SubmittedDate = DateTime.Now // You can add a CreatedAt field later
                })
                .ToListAsync();

            // Get pending products
            var pendingProducts = await _context.Products
                .Include(p => p.Restaurant)
                .Include(p => p.Category)
                .Where(p => p.IsApproved == 0)
                .Select(p => new PendingProduct
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    ImagePath = p.ImagePath,
                    Price = p.Price,
                    Stock = p.Stock,
                    RestaurantName = p.Restaurant.Name,
                    CategoryName = p.Category.Name,
                    SubmittedDate = DateTime.Now // You can add a CreatedAt field later
                })
                .ToListAsync();

            viewModel.PendingRestaurants = pendingRestaurants;
            viewModel.PendingProducts = pendingProducts;

            return View(viewModel);
        }

        // Approve Restaurant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRestaurant(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant nu a fost găsit.";
                return RedirectToAction(nameof(Notifications));
            }

            restaurant.IsApproved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Restaurantul '{restaurant.Name}' a fost aprobat cu succes!";
            return RedirectToAction(nameof(Notifications));
        }

        // Reject Restaurant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRestaurant(int id)
        {
            var restaurant = await _context.Restaurants.Include(r => r.Products).FirstOrDefaultAsync(r => r.Id == id);
            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant nu a fost găsit.";
                return RedirectToAction(nameof(Notifications));
            }

            // Delete associated products first
            _context.Products.RemoveRange(restaurant.Products);
            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Restaurantul '{restaurant.Name}' a fost respins și șters.";
            return RedirectToAction(nameof(Notifications));
        }

        // Approve Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Produsul nu a fost găsit.";
                return RedirectToAction(nameof(Notifications));
            }

            product.IsApproved = 1;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Produsul '{product.Title}' a fost aprobat cu succes!";
            return RedirectToAction(nameof(Notifications));
        }

        // Reject Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Produsul nu a fost găsit.";
                return RedirectToAction(nameof(Notifications));
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Produsul '{product.Title}' a fost respins și șters.";
            return RedirectToAction(nameof(Notifications));
        }
    }
}
