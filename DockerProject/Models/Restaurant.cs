using System.ComponentModel.DataAnnotations;

namespace DockerProject.Models;

public class Restaurant
{
    public int Id { get; set; }
    [Required] public string Name { get; set; }
    [Required] public string ImagePath { get; set; }
    [Required] public bool IsApproved { get; set; }
    public string OwnerId { get; set; }
    public ApplicationUser Owner { get; set; }
    public virtual ICollection<Product> Products { get; set; }
    public virtual ICollection<RestaurantRating>? Ratings { get; set; }
}