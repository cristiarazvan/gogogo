using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using DockerProject.Data;
using DockerProject.Models;

namespace DockerProject.Controllers
{
    [Authorize]
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RestaurantsController(ApplicationDbContext context, 
                                     UserManager<ApplicationUser> userManager,
                                     IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Restaurants
        [AllowAnonymous]
        public async Task<IActionResult> Index(string sortOrder, string[] categoryFilter, string searchString)
        {
            // 1. Populăm categoriile
            ViewBag.Categories = await _context.Categories
                                               .Select(c => c.Name)
                                               .Distinct()
                                               .ToListAsync();

            // Păstrăm parametrii în ViewBag
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentFilter = categoryFilter;
            ViewBag.CurrentSearch = searchString; // <--- Păstrăm ce a scris userul în search bar

            // 2. Query de Bază
            var restaurantsQuery = _context.Restaurants
                .Include(r => r.Owner)
                .Include(r => r.Ratings)
                .Include(r => r.Products)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // --- LOGICA DE CĂUTARE (SMART SEARCH) ---
            if (!string.IsNullOrEmpty(searchString))
            {
                // Căutăm textul în Numele Restaurantului SAU în Titlul Produselor SAU în Descrierea Produselor
                restaurantsQuery = restaurantsQuery.Where(r => 
                    r.Name.Contains(searchString) || 
                    r.Products.Any(p => p.Title.Contains(searchString) || p.Description.Contains(searchString))
                );
            }

            // 3. APLICARE FILTRU (Categorie)
            if (categoryFilter != null && categoryFilter.Length > 0)
            {
                restaurantsQuery = restaurantsQuery.Where(r => r.Products.Any(p => categoryFilter.Contains(p.Category.Name)));
            }

            // Aducem datele în memorie
            var restaurantsList = await restaurantsQuery.ToListAsync();

            // 4. APLICARE SORTARE
            if (!string.IsNullOrEmpty(sortOrder))
            {
                switch (sortOrder)
                {
                    case "RatingDesc":
                        restaurantsList = restaurantsList.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0).ToList();
                        break;
                    case "RatingAsc":
                        restaurantsList = restaurantsList.OrderBy(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0).ToList();
                        break;
                    case "NameAsc":
                        restaurantsList = restaurantsList.OrderBy(r => r.Name).ToList();
                        break;
                    case "PriceAsc":
                        // Sort by average product price (most affordable first)
                        // Restaurants with no products go to the end
                        restaurantsList = restaurantsList.OrderBy(r => r.Products.Any() ? r.Products.Average(p => p.Price) : double.MaxValue).ToList();
                        break;
                    case "PriceDesc":
                        // Sort by average product price (most expensive first)
                        // Restaurants with no products go to the end
                        restaurantsList = restaurantsList.OrderByDescending(r => r.Products.Any() ? r.Products.Average(p => p.Price) : 0).ToList();
                        break;
                    default:
                        restaurantsList = restaurantsList.OrderBy(r => r.Name).ToList();
                        break;
                }
            }
            else
            {
                // Sortare Default
                if (User.Identity.IsAuthenticated)
                {
                    var currentUserId = _userManager.GetUserId(User);
                    restaurantsList = restaurantsList
                        .OrderByDescending(r => r.OwnerId == currentUserId)
                        .ThenByDescending(r => r.IsApproved == 1)
                        .ThenBy(r => r.Name)
                        .ToList();
                }
                else
                {
                    restaurantsList = restaurantsList
                        .OrderByDescending(r => r.IsApproved == 1)
                        .ThenBy(r => r.Name)
                        .ToList();
                }
            }

            return View(restaurantsList);
        } 
        
        // GET: Restaurants/Details
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .Include(r => r.Products)
                .Include(r => r.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (restaurant == null) return NotFound();

            double averageRating = 0;
            int ratingCount = 0;
    
            if (restaurant.Ratings != null && restaurant.Ratings.Any())
            {
                averageRating = restaurant.Ratings.Average(r => r.Score);
                ratingCount = restaurant.Ratings.Count;
            }

            int userRating = 0;
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var existingRating = restaurant.Ratings?.FirstOrDefault(r => r.UserId == userId);
                if (existingRating != null)
                {
                    userRating = existingRating.Score;
                }
            }

            ViewBag.AverageRating = averageRating;
            ViewBag.RatingCount = ratingCount;
            ViewBag.UserRating = userRating;
            
            return View(restaurant);
        }

        // GET: Restaurants/Create
        [Authorize(Roles = "Admin,Collaborator")]
        public async Task<IActionResult> Create()
        {
            // Doar Adminul are nevoie de lista de useri
            if (User.IsInRole("Admin"))
            {
                var allUsers = await _context.Users.ToListAsync();

                // Folosim cheia "OwnerList" ca să nu se confunde cu proprietatea OwnerId
                ViewData["OwnerList"] = new SelectList(allUsers, "Id", "UserName");
            }

            // Pass role info to the view for displaying appropriate messages
            ViewBag.IsCollaborator = User.IsInRole("Collaborator");

            return View();
        }

        // POST: Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Collaborator")]
        public async Task<IActionResult> Create(Restaurant restaurant, IFormFile? imageFile)
        {
            // LOGICA OWNER & STATUS
            bool isCollaborator = User.IsInRole("Collaborator");
            if (!User.IsInRole("Admin"))
            {
                restaurant.OwnerId = _userManager.GetUserId(User);
                // Collaborators submit restaurants for approval (0=Pending)
                restaurant.IsApproved = 0;
            }
            else
            {
                if (string.IsNullOrEmpty(restaurant.OwnerId)) restaurant.OwnerId = _userManager.GetUserId(User);
            }

            // --- FIX 1: SCOATEM VALIDAREA AUTOMATA PENTRU IMAGINE ---
            ModelState.Remove("ImagePath"); // <--- Asta rezolva eroarea "nu ai completat poza"
            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");
            ModelState.Remove("Products");
            ModelState.Remove("Ratings");

            // --- PROCESARE IMAGINE ---
            if (imageFile != null && imageFile.Length > 0)
            {
                // Validari dimensiune/tip...
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImagePath", "Image too large (Max 5MB).");
                    if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
                    ViewBag.IsCollaborator = isCollaborator;
                    return View(restaurant);
                }

                // --- FIX 2: CREARE FOLDER (Siguranta) ---
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "restaurants");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder); // Cream folderul daca nu exista
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                restaurant.ImagePath = "/images/restaurants/" + uniqueFileName;
            }
            else
            {
                // Daca e CREATE si nu avem poza, e o problema (ca e Required)
                ModelState.AddModelError("ImagePath", "Please upload an image.");
                if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
                ViewBag.IsCollaborator = isCollaborator;
                return View(restaurant);
            }

            if (ModelState.IsValid)
            {
                _context.Add(restaurant);
                await _context.SaveChangesAsync();

                // Set success message based on role
                if (isCollaborator)
                {
                    TempData["Success"] = $"Restaurant '{restaurant.Name}' has been submitted for approval. An administrator will review your request.";
                }
                else
                {
                    TempData["Success"] = $"Restaurant '{restaurant.Name}' has been created successfully.";
                }

                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
            ViewBag.IsCollaborator = isCollaborator;
            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            var userId = _userManager.GetUserId(User);
    
            if (restaurant.OwnerId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (User.IsInRole("Admin"))
            {
                var allUsers = await _context.Users.ToListAsync();
                ViewData["OwnerList"] = new SelectList(allUsers, "Id", "UserName", restaurant.OwnerId);
            }

            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Restaurant restaurant, IFormFile? imageFile)
        {
            if (id != restaurant.Id) return NotFound();

            // --- FIX 1: VALIDĂRI ---
            ModelState.Remove("ImagePath"); // Ignorăm validarea string-ului
            ModelState.Remove("Owner");
            ModelState.Remove("Products");
            ModelState.Remove("Ratings");

            var oldData = await _context.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (oldData == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                restaurant.OwnerId = oldData.OwnerId;
                restaurant.IsApproved = oldData.IsApproved;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                // --- FIX 2: CREARE FOLDER ---
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "restaurants");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                restaurant.ImagePath = "/images/restaurants/" + uniqueFileName;
            }
            else
            {
                // Păstrăm calea veche
                restaurant.ImagePath = oldData.ImagePath;
            }

            if (ModelState.IsValid)
            {
                try { _context.Update(restaurant); await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException) { if (!_context.Restaurants.Any(e => e.Id == restaurant.Id)) return NotFound(); else throw; }
                return RedirectToAction(nameof(Index));
            }
    
            if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (restaurant == null) return NotFound();

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRating(int restaurantId, int score)
        {
            if (score < 1 || score > 5) return BadRequest("Invalid score");

            var userId = _userManager.GetUserId(User);

            var existingRating = await _context.RestaurantRating
                .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId && r.UserId == userId);

            if (existingRating != null)
            {
                existingRating.Score = score;
                _context.Update(existingRating);
            }
            else
            {
                var newRating = new RestaurantRating
                {
                    RestaurantId = restaurantId,
                    UserId = userId,
                    Score = score
                };
                _context.Add(newRating);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = restaurantId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawRating(int restaurantId)
        {
            var userId = _userManager.GetUserId(User);

            var existingRating = await _context.RestaurantRating
                .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId && r.UserId == userId);

            if (existingRating != null)
            {
                _context.RestaurantRating.Remove(existingRating);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = restaurantId });
        }
    }
}