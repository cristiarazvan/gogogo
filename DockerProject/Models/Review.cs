using System.ComponentModel.DataAnnotations;

namespace DockerProject.Models;

public class Review
{
    public int Id { get; set; }
    public string? Text { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; }
    
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
}