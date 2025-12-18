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
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Restaurants.Include(r => r.Owner);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Restaurants/Details/5
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

            return View(restaurant);
        }

        // GET: Restaurants/Create
        public async Task<IActionResult> Create()
        {
            // Doar Adminul are nevoie de lista de useri
            if (User.IsInRole("Admin"))
            {
                var allUsers = await _context.Users.ToListAsync();
        
                // Folosim cheia "OwnerList" ca să nu se confunde cu proprietatea OwnerId
                ViewData["OwnerList"] = new SelectList(allUsers, "Id", "UserName");
            }
            return View();
        }

        // POST: Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Restaurant restaurant, IFormFile? imageFile)
        {
            // LOGICA OWNER & STATUS
            if (!User.IsInRole("Admin"))
            {
                restaurant.OwnerId = _userManager.GetUserId(User);
                restaurant.IsApproved = 2; 
            }
            else 
            {
                if (string.IsNullOrEmpty(restaurant.OwnerId)) restaurant.OwnerId = _userManager.GetUserId(User);
            }

            // --- FIX 1: SCOATEM VALIDAREA AUTOMATĂ PENTRU IMAGINE ---
            ModelState.Remove("ImagePath"); // <--- Asta rezolvă eroarea "nu ai completat poza"
            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");
            ModelState.Remove("Products");
            ModelState.Remove("Ratings");

            // --- PROCESARE IMAGINE ---
            if (imageFile != null && imageFile.Length > 0)
            {
                // Validări dimensiune/tip...
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImagePath", "Image too large (Max 5MB).");
                    if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
                    return View(restaurant);
                }

                // --- FIX 2: CREARE FOLDER (Siguranță) ---
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "restaurants");
                if (!Directory.Exists(uploadsFolder)) 
                {
                    Directory.CreateDirectory(uploadsFolder); // Creăm folderul dacă nu există
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
                // Dacă e CREATE și nu avem poză, e o problemă (că e Required)
                ModelState.AddModelError("ImagePath", "Please upload an image.");
                if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
                return View(restaurant);
            }

            if (ModelState.IsValid)
            {
                _context.Add(restaurant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Admin")) ViewData["OwnerList"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
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
    }
}