namespace DockerProject.Models
{
    public class NotificationsViewModel
    {
        public List<PendingRestaurant> PendingRestaurants { get; set; } = new List<PendingRestaurant>();
        public List<PendingRestaurant> DeniedRestaurants { get; set; } = new List<PendingRestaurant>();
        public List<PendingProduct> PendingProducts { get; set; } = new List<PendingProduct>();
    }

    public class PendingRestaurant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public DateTime SubmittedDate { get; set; }
    }

    public class PendingProduct
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
        public string RestaurantName { get; set; }
        public string CategoryName { get; set; }
        public DateTime SubmittedDate { get; set; }
    }
}
