using System.ComponentModel.DataAnnotations;

namespace DockerProject.Models;

public class Product
{
    public int Id { get; set; }
    [Required] public string Title { get; set; }
    [Range(0.01, 10000)] public double Price { get; set; }
    [Range(0, 10000)] public int Stock { get; set; }
    public int RestaurantId { get; set; }
    public virtual Restaurant? Restaurant { get; set; }
}