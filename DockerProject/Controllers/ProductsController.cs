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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Products
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var products = _context.Products.Include(p => p.Category).Include(p => p.Restaurant);
            return View(await products.ToListAsync());
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Restaurant)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();
            
            var currentUserId = _userManager.GetUserId(User);
    
            if (currentUserId != null && product.Reviews != null)
            {
                product.Reviews = product.Reviews
                    .OrderByDescending(r => r.UserId == currentUserId)
                    .ThenByDescending(r => r.Id)
                    .ToList();
            }

            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            
            // LOGICA NOUA: Admin vede toate restaurantele, User vede doar ale lui
            IQueryable<Restaurant> restaurantsQuery = _context.Restaurants;
            
            if (!User.IsInRole("Admin"))
            {
                restaurantsQuery = restaurantsQuery.Where(r => r.OwnerId == userId);
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["RestaurantId"] = new SelectList(await restaurantsQuery.ToListAsync(), "Id", "Name");
            
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // 1. Curățăm validările automate
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("Restaurant");
            ModelState.Remove("Reviews");

            // 2. Verificăm permisiunile (dacă nu e Admin)
            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                var restaurant = await _context.Restaurants.FindAsync(product.RestaurantId);
                if (restaurant == null || restaurant.OwnerId != userId)
                {
                    return Forbid();
                }
            }

            // 3. PROCESAREA IMAGINII (AICI E FIX-UL)
            if (imageFile != null && imageFile.Length > 0)
            {
                // Validare mărime
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                     ModelState.AddModelError("ImagePath", "Image too large.");
                     // Reîncărcare liste...
                     var uid = _userManager.GetUserId(User);
                     IQueryable<Restaurant> q = _context.Restaurants;
                     if (!User.IsInRole("Admin")) q = q.Where(r => r.OwnerId == uid);
                     ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                     ViewData["RestaurantId"] = new SelectList(await q.ToListAsync(), "Id", "Name", product.RestaurantId);
                     return View(product);
                }

                // --- FIX CRITIC: CREAREA FOLDERULUI ---
                // Construim calea către folderul 'products'
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                
                // Verificăm dacă folderul există. Dacă NU, îl creăm acum!
                if (!Directory.Exists(uploadsFolder)) 
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                // -------------------------------------

                // Acum putem salva fișierul liniștiți
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                
                // Salvăm calea relativă în baza de date
                product.ImagePath = "/images/products/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            // Reîncărcare liste la eroare
            var currentUserId = _userManager.GetUserId(User);
            IQueryable<Restaurant> repoQuery = _context.Restaurants;
            if (!User.IsInRole("Admin")) repoQuery = repoQuery.Where(r => r.OwnerId == currentUserId);

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["RestaurantId"] = new SelectList(await repoQuery.ToListAsync(), "Id", "Name", product.RestaurantId);
            return View(product);
        }
        
        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Verificare permisiuni
            var userId = _userManager.GetUserId(User);
            var restaurant = await _context.Restaurants.FindAsync(product.RestaurantId);
            
            // Daca nu e Admin si nici proprietarul restaurantului produsului -> Afara
            if (!User.IsInRole("Admin") && (restaurant == null || restaurant.OwnerId != userId))
            {
                return Forbid();
            }

            // Dropdown Restaurante (Admin vede tot, User vede doar ale lui)
            IQueryable<Restaurant> restaurantsQuery = _context.Restaurants;
            if (!User.IsInRole("Admin"))
            {
                restaurantsQuery = restaurantsQuery.Where(r => r.OwnerId == userId);
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["RestaurantId"] = new SelectList(await restaurantsQuery.ToListAsync(), "Id", "Name", product.RestaurantId);
            
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return NotFound();

            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("Restaurant");
            ModelState.Remove("Reviews");

            if (imageFile != null && imageFile.Length > 0)
            {
                // --- FIX CRITIC: CREAREA FOLDERULUI SI LA EDIT ---
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder)) 
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                // ------------------------------------------------

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                product.ImagePath = "/images/products/" + uniqueFileName;
            }
            else
            {
                // Păstrăm imaginea veche
                var oldData = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                product.ImagePath = oldData.ImagePath;
            }

            if (ModelState.IsValid)
            {
                try 
                { 
                    _context.Update(product); 
                    await _context.SaveChangesAsync(); 
                }
                catch (DbUpdateConcurrencyException) 
                { 
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound(); 
                    else throw; 
                }
                return RedirectToAction(nameof(Index));
            }
            
            // Reîncărcare liste
            var userId = _userManager.GetUserId(User);
            IQueryable<Restaurant> finalQuery = _context.Restaurants;
            if (!User.IsInRole("Admin")) finalQuery = finalQuery.Where(r => r.OwnerId == userId);
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["RestaurantId"] = new SelectList(await finalQuery.ToListAsync(), "Id", "Name", product.RestaurantId);
            return View(product);
        }
        
        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, string reviewText)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
            {
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = _userManager.GetUserId(User)!,
                Text = reviewText
                // Data = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = productId });
        }
            
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int reviewId, int productId, string newText)
        {
            if (string.IsNullOrWhiteSpace(newText))
            {
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var review = await _context.Reviews.FindAsync(reviewId);
            var currentUserId = _userManager.GetUserId(User);

            if (review != null && (review.UserId == currentUserId || User.IsInRole("Admin")))
            {
                review.Text = newText;
                // review.Date = DateTime.Now;
                _context.Update(review);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = productId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId, int productId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            var currentUserId = _userManager.GetUserId(User);

            if (review != null && (review.UserId == currentUserId || User.IsInRole("Admin")))
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = productId });
        }
    }
}