using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DockerProject.Models
{
    public class RestaurantRating
    {
        public int Id { get; set; }

        [Range(1, 5)] public int Score { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }
    }
}