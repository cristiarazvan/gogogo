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
            ModelState.Remove("Category");
            ModelState.Remove("Restaurant");
            ModelState.Remove("Reviews");

            // Validare suplimentara: Un user simplu nu are voie sa puna produse la restaurantul altuia
            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                var restaurant = await _context.Restaurants.FindAsync(product.RestaurantId);
                if (restaurant == null || restaurant.OwnerId != userId)
                {
                    return Forbid();
                }
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                product.ImagePath = "/images/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            // Reincarcam listele in caz de eroare
            var currentUserId = _userManager.GetUserId(User);
            IQueryable<Restaurant> repoQuery = _context.Restaurants;
            if (!User.IsInRole("Admin"))
            {
                repoQuery = repoQuery.Where(r => r.OwnerId == currentUserId);
            }

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

            ModelState.Remove("Category");
            ModelState.Remove("Restaurant");
            ModelState.Remove("Reviews");

            if (ModelState.IsValid)
            {
                try
                {
                    // Pastram imaginea veche daca nu se incarca una noua
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        product.ImagePath = "/images/" + uniqueFileName;
                    }
                    else
                    {
                        var oldData = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                        product.ImagePath = oldData.ImagePath;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            // Reincarcare liste la eroare
            var userId = _userManager.GetUserId(User);
            IQueryable<Restaurant> repoQuery = _context.Restaurants;
            if (!User.IsInRole("Admin"))
            {
                repoQuery = repoQuery.Where(r => r.OwnerId == userId);
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["RestaurantId"] = new SelectList(await repoQuery.ToListAsync(), "Id", "Name", product.RestaurantId);
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
    }
}