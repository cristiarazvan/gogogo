using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DockerProject.Models;

namespace DockerProject.Controllers
{
    [Authorize(Roles = "Admin")] // <--- Doar Adminii au acces aici
    public class AdminsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
    }
}