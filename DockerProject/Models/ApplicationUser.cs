using Microsoft.AspNetCore.Identity;

namespace DockerProject.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
    
    public virtual ICollection<Restaurant> OwnedRestaurants { get; set; }
}