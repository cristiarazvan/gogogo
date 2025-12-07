namespace DockerProject.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public DateTime Date { get; set; }
    public double Total { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; }
}