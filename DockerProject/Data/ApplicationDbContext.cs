using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DockerProject.Models;

namespace DockerProject.Data
{
    // AICI este modificarea critică: <ApplicationUser>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tabelele aplicației
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RestaurantRating> RestaurantRating { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // IMPORTANT: Această linie este obligatorie pentru Identity!
            base.OnModelCreating(builder);

            // Configurare pentru compatibilitate MySQL (din template-ul tău)
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string))
                    {
                        var maxLength = property.GetMaxLength();
                        if (maxLength == null)
                        {
                            // Setează lungimea maximă default pentru cheile primare și străine de tip string
                            if (property.IsKey() || property.IsForeignKey())
                            {
                                property.SetMaxLength(255);
                            }
                        }
                    }
                }
            }
        }
    }
}