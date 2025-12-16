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
            // Daca nu e Admin, proprietarul este automat cel logat
            if (!User.IsInRole("Admin"))
            {
                restaurant.OwnerId = _userManager.GetUserId(User);
            }
            else 
            {
                // Daca e Admin si nu a selectat nimic din dropdown, se pune pe el insusi
                if (string.IsNullOrEmpty(restaurant.OwnerId))
                {
                    restaurant.OwnerId = _userManager.GetUserId(User);
                }
            }

            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId"); // Il scoatem de la validare, ca poate veni gol din form
            ModelState.Remove("Products");
            ModelState.Remove("Ratings");

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
                restaurant.ImagePath = "/images/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                // Set new restaurants as pending by default (0 = Pending)
                restaurant.IsApproved = 0;
                _context.Add(restaurant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Reincarcam lista de Owneri daca e Admin si a dat eroare
            if (User.IsInRole("Admin"))
            {
                ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
            }

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

            ModelState.Remove("Owner");
            ModelState.Remove("Products");
            ModelState.Remove("Ratings");
            
            // Daca userul NU e admin, nu are voie sa schimbe Owner-ul, deci il fortam pe cel vechi
            if (!User.IsInRole("Admin"))
            {
                 var original = await _context.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                 restaurant.OwnerId = original?.OwnerId;
            }
            // Daca E admin, OwnerId vine din Formular (dropdown), deci e ok.

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        restaurant.ImagePath = "/images/" + uniqueFileName;
                    }
                    else
                    {
                        var oldData = await _context.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                        restaurant.ImagePath = oldData.ImagePath;
                    }

                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            if (User.IsInRole("Admin"))
            {
                ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", restaurant.OwnerId);
            }

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