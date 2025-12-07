using System.ComponentModel.DataAnnotations;

namespace DockerProject.Models;

public class Restaurant
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string OwnerId { get; set; }
    public bool IsApproved { get; set; }
    public virtual ICollection<Product> Products { get; set; }
}