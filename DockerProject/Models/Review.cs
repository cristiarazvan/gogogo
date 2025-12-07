using System.ComponentModel.DataAnnotations;

namespace DockerProject.Models;

public class Review
{
    public int Id { get; set; }
    public string Text { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; }
}