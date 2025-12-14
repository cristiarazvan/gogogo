using System.ComponentModel.DataAnnotations;

namespace DockerProject.Models;

public class Product
{
    public int Id { get; set; }
    [Required] public string Title { get; set; }
    [Required] public string Description { get; set; }
    [Required] public string ImagePath { get; set; }
    [Range(0.01, 10000)] public double Price { get; set; }
    [Range(0, 10000)] public int Stock { get; set; }
    [Range(0, 2)] public int IsApproved { get; set; }
    public int RestaurantId { get; set; }
    public virtual Restaurant Restaurant { get; set; }
    
    
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; }
    
    public virtual ICollection<Review>? Reviews { get; set; }
    
}