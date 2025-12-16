using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DockerProject.Models;

namespace DockerProject.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = serviceProvider.GetRequiredService<ApplicationDbContext>())
            {
                // Verificăm dacă există deja utilizatori pentru a nu rula seed-ul de două ori
                if (context.Users.Any())
                {
                    return;
                }

                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // 1. Definim Rolurile
                string[] roleNames = { "Admin", "Collaborator", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // 2. Creăm Administratorul
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@test.com",
                    Email = "admin@test.com",
                    FullName = "Admin Principal",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Parola!123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // 3. Creăm Colaboratori (Proprietari de Restaurante)
                var chef1 = new ApplicationUser
                {
                    UserName = "gordon@ramsay.com",
                    Email = "gordon@ramsay.com",
                    FullName = "Gordon Ramsay",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(chef1, "Parola!123");
                await userManager.AddToRoleAsync(chef1, "Collaborator");

                var chef2 = new ApplicationUser
                {
                    UserName = "jamie@oliver.com",
                    Email = "jamie@oliver.com",
                    FullName = "Jamie Oliver",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(chef2, "Parola!123");
                await userManager.AddToRoleAsync(chef2, "Collaborator");

                var chef3 = new ApplicationUser
                {
                    UserName = "maria@pizza.com",
                    Email = "maria@pizza.com",
                    FullName = "Maria Rossi",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(chef3, "Parola!123");
                await userManager.AddToRoleAsync(chef3, "Collaborator");

                var chef4 = new ApplicationUser
                {
                    UserName = "chen@wok.com",
                    Email = "chen@wok.com",
                    FullName = "Chen Wei",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(chef4, "Parola!123");
                await userManager.AddToRoleAsync(chef4, "Collaborator");

                var chef5 = new ApplicationUser
                {
                    UserName = "pierre@bistro.com",
                    Email = "pierre@bistro.com",
                    FullName = "Pierre Dubois",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(chef5, "Parola!123");
                await userManager.AddToRoleAsync(chef5, "Collaborator");

                // 4. Creăm Utilizatori Simpli (Clienți)
                var user1 = new ApplicationUser
                {
                    UserName = "john@email.com",
                    Email = "john@email.com",
                    FullName = "John Smith",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user1, "Parola!123");
                await userManager.AddToRoleAsync(user1, "User");

                var user2 = new ApplicationUser
                {
                    UserName = "emma@email.com",
                    Email = "emma@email.com",
                    FullName = "Emma Johnson",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user2, "Parola!123");
                await userManager.AddToRoleAsync(user2, "User");

                var user3 = new ApplicationUser
                {
                    UserName = "alex@email.com",
                    Email = "alex@email.com",
                    FullName = "Alex Brown",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user3, "Parola!123");
                await userManager.AddToRoleAsync(user3, "User");

                var user4 = new ApplicationUser
                {
                    UserName = "sarah@email.com",
                    Email = "sarah@email.com",
                    FullName = "Sarah Williams",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user4, "Parola!123");
                await userManager.AddToRoleAsync(user4, "User");

                var user5 = new ApplicationUser
                {
                    UserName = "michael@email.com",
                    Email = "michael@email.com",
                    FullName = "Michael Davis",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user5, "Parola!123");
                await userManager.AddToRoleAsync(user5, "User");

                // 5. Creăm Categorii
                var categories = new List<Category>
                {
                    new Category { Name = "Pizza" },
                    new Category { Name = "Burger" },
                    new Category { Name = "Pasta" },
                    new Category { Name = "Sushi" },
                    new Category { Name = "Chinese" },
                    new Category { Name = "Indian" },
                    new Category { Name = "Salads" },
                    new Category { Name = "Desserts" },
                    new Category { Name = "Beverages" },
                    new Category { Name = "Breakfast" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();

                // 6. Creăm Restaurante
                var restaurant1 = new Restaurant
                {
                    Name = "Gordon's Kitchen",
                    ImagePath = "/images/restaurants/gordons-kitchen.jpg",
                    OwnerId = chef1.Id,
                    IsApproved = 1 // Approved
                };

                var restaurant2 = new Restaurant
                {
                    Name = "Jamie's Italian",
                    ImagePath = "/images/restaurants/jamies-italian.jpg",
                    OwnerId = chef2.Id,
                    IsApproved = 1 // Approved
                };

                var restaurant3 = new Restaurant
                {
                    Name = "La Bella Pizza",
                    ImagePath = "/images/restaurants/bella-pizza.jpg",
                    OwnerId = chef3.Id,
                    IsApproved = 1 // Approved
                };

                var restaurant4 = new Restaurant
                {
                    Name = "Golden Wok",
                    ImagePath = "/images/restaurants/golden-wok.jpg",
                    OwnerId = chef4.Id,
                    IsApproved = 1 // Approved
                };

                var restaurant5 = new Restaurant
                {
                    Name = "Le Petit Bistro",
                    ImagePath = "/images/restaurants/petit-bistro.jpg",
                    OwnerId = chef5.Id,
                    IsApproved = 0 // Pending approval
                };

                var restaurant6 = new Restaurant
                {
                    Name = "Burger Palace",
                    ImagePath = "/images/restaurants/burger-palace.jpg",
                    OwnerId = chef1.Id,
                    IsApproved = 1 // Approved
                };

                await context.Restaurants.AddRangeAsync(restaurant1, restaurant2, restaurant3, restaurant4, restaurant5, restaurant6);
                await context.SaveChangesAsync();

                // 7. Creăm Produse pentru fiecare restaurant
                var products = new List<Product>
                {
                    // Gordon's Kitchen - Fine Dining
                    new Product { Title = "Beef Wellington", Description = "Premium beef wrapped in puff pastry", ImagePath = "/images/products/beef-wellington.jpg", Price = 45.99, Stock = 20, IsApproved = 1, RestaurantId = restaurant1.Id, CategoryId = categories[2].Id },
                    new Product { Title = "Lobster Thermidor", Description = "Classic French lobster dish", ImagePath = "/images/products/lobster.jpg", Price = 55.99, Stock = 15, IsApproved = 1, RestaurantId = restaurant1.Id, CategoryId = categories[2].Id },
                    new Product { Title = "Caesar Salad", Description = "Fresh romaine with parmesan", ImagePath = "/images/products/caesar-salad.jpg", Price = 12.99, Stock = 50, IsApproved = 1, RestaurantId = restaurant1.Id, CategoryId = categories[6].Id },
                    new Product { Title = "Sticky Toffee Pudding", Description = "Traditional British dessert", ImagePath = "/images/products/toffee-pudding.jpg", Price = 8.99, Stock = 30, IsApproved = 1, RestaurantId = restaurant1.Id, CategoryId = categories[7].Id },

                    // Jamie's Italian
                    new Product { Title = "Spaghetti Carbonara", Description = "Creamy Italian classic", ImagePath = "/images/products/carbonara.jpg", Price = 16.99, Stock = 40, IsApproved = 1, RestaurantId = restaurant2.Id, CategoryId = categories[2].Id },
                    new Product { Title = "Margherita Pizza", Description = "Fresh mozzarella and basil", ImagePath = "/images/products/margherita.jpg", Price = 14.99, Stock = 50, IsApproved = 1, RestaurantId = restaurant2.Id, CategoryId = categories[0].Id },
                    new Product { Title = "Lasagna al Forno", Description = "Layers of pasta and bolognese", ImagePath = "/images/products/lasagna.jpg", Price = 18.99, Stock = 25, IsApproved = 1, RestaurantId = restaurant2.Id, CategoryId = categories[2].Id },
                    new Product { Title = "Tiramisu", Description = "Coffee-soaked Italian dessert", ImagePath = "/images/products/tiramisu.jpg", Price = 7.99, Stock = 35, IsApproved = 1, RestaurantId = restaurant2.Id, CategoryId = categories[7].Id },

                    // La Bella Pizza
                    new Product { Title = "Quattro Formaggi", Description = "Four cheese pizza", ImagePath = "/images/products/quattro-formaggi.jpg", Price = 17.99, Stock = 45, IsApproved = 1, RestaurantId = restaurant3.Id, CategoryId = categories[0].Id },
                    new Product { Title = "Pepperoni Pizza", Description = "Classic pepperoni pizza", ImagePath = "/images/products/pepperoni.jpg", Price = 15.99, Stock = 60, IsApproved = 1, RestaurantId = restaurant3.Id, CategoryId = categories[0].Id },
                    new Product { Title = "Diavola Pizza", Description = "Spicy salami pizza", ImagePath = "/images/products/diavola.jpg", Price = 16.99, Stock = 40, IsApproved = 1, RestaurantId = restaurant3.Id, CategoryId = categories[0].Id },
                    new Product { Title = "Caprese Salad", Description = "Tomato, mozzarella, basil", ImagePath = "/images/products/caprese.jpg", Price = 9.99, Stock = 30, IsApproved = 1, RestaurantId = restaurant3.Id, CategoryId = categories[6].Id },

                    // Golden Wok - Chinese
                    new Product { Title = "Kung Pao Chicken", Description = "Spicy stir-fried chicken", ImagePath = "/images/products/kung-pao.jpg", Price = 13.99, Stock = 35, IsApproved = 1, RestaurantId = restaurant4.Id, CategoryId = categories[4].Id },
                    new Product { Title = "Sweet and Sour Pork", Description = "Classic Chinese favorite", ImagePath = "/images/products/sweet-sour.jpg", Price = 14.99, Stock = 40, IsApproved = 1, RestaurantId = restaurant4.Id, CategoryId = categories[4].Id },
                    new Product { Title = "Fried Rice", Description = "Egg fried rice with vegetables", ImagePath = "/images/products/fried-rice.jpg", Price = 8.99, Stock = 50, IsApproved = 1, RestaurantId = restaurant4.Id, CategoryId = categories[4].Id },
                    new Product { Title = "Spring Rolls", Description = "Crispy vegetable rolls", ImagePath = "/images/products/spring-rolls.jpg", Price = 6.99, Stock = 60, IsApproved = 1, RestaurantId = restaurant4.Id, CategoryId = categories[4].Id },

                    // Le Petit Bistro - French (Not approved restaurant)
                    new Product { Title = "Coq au Vin", Description = "Chicken braised in wine", ImagePath = "/images/products/coq-au-vin.jpg", Price = 22.99, Stock = 20, IsApproved = 0, RestaurantId = restaurant5.Id, CategoryId = categories[2].Id },
                    new Product { Title = "Ratatouille", Description = "Provençal vegetable stew", ImagePath = "/images/products/ratatouille.jpg", Price = 15.99, Stock = 25, IsApproved = 0, RestaurantId = restaurant5.Id, CategoryId = categories[6].Id },

                    // Burger Palace
                    new Product { Title = "Classic Cheeseburger", Description = "100% beef with cheddar", ImagePath = "/images/products/cheeseburger.jpg", Price = 11.99, Stock = 70, IsApproved = 1, RestaurantId = restaurant6.Id, CategoryId = categories[1].Id },
                    new Product { Title = "Bacon Burger", Description = "Beef burger with crispy bacon", ImagePath = "/images/products/bacon-burger.jpg", Price = 13.99, Stock = 55, IsApproved = 1, RestaurantId = restaurant6.Id, CategoryId = categories[1].Id },
                    new Product { Title = "Veggie Burger", Description = "Plant-based patty", ImagePath = "/images/products/veggie-burger.jpg", Price = 10.99, Stock = 40, IsApproved = 1, RestaurantId = restaurant6.Id, CategoryId = categories[1].Id },
                    new Product { Title = "French Fries", Description = "Crispy golden fries", ImagePath = "/images/products/fries.jpg", Price = 4.99, Stock = 100, IsApproved = 1, RestaurantId = restaurant6.Id, CategoryId = categories[6].Id },
                    new Product { Title = "Milkshake", Description = "Vanilla milkshake", ImagePath = "/images/products/milkshake.jpg", Price = 5.99, Stock = 50, IsApproved = 1, RestaurantId = restaurant6.Id, CategoryId = categories[8].Id }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();

                // 8. Creăm Reviews pentru produse
                var reviews = new List<Review>
                {
                    new Review { Text = "Absolutely amazing! Best beef wellington I've ever had.", ProductId = products[0].Id, UserId = user1.Id },
                    new Review { Text = "Delicious lobster, perfectly cooked.", ProductId = products[1].Id, UserId = user2.Id },
                    new Review { Text = "Great carbonara, just like in Italy!", ProductId = products[4].Id, UserId = user1.Id },
                    new Review { Text = "Perfect pizza, crispy crust and fresh ingredients.", ProductId = products[5].Id, UserId = user3.Id },
                    new Review { Text = "Best margherita in town!", ProductId = products[5].Id, UserId = user4.Id },
                    new Review { Text = "The tiramisu is to die for!", ProductId = products[7].Id, UserId = user2.Id },
                    new Review { Text = "Love the four cheese pizza, so creamy!", ProductId = products[8].Id, UserId = user5.Id },
                    new Review { Text = "Pepperoni pizza was good but a bit too salty.", ProductId = products[9].Id, UserId = user1.Id },
                    new Review { Text = "Kung Pao chicken had the perfect amount of spice!", ProductId = products[12].Id, UserId = user3.Id },
                    new Review { Text = "Sweet and sour pork was delicious and tender.", ProductId = products[13].Id, UserId = user4.Id },
                    new Review { Text = "Classic cheeseburger - simple and satisfying.", ProductId = products[18].Id, UserId = user5.Id },
                    new Review { Text = "Bacon burger was juicy and flavorful!", ProductId = products[19].Id, UserId = user2.Id },
                    new Review { Text = "Great veggie burger option for vegetarians.", ProductId = products[20].Id, UserId = user1.Id },
                    new Review { Text = "Crispy fries, perfect side dish!", ProductId = products[21].Id, UserId = user3.Id }
                };

                await context.Reviews.AddRangeAsync(reviews);
                await context.SaveChangesAsync();

                // 9. Creăm Restaurant Ratings
                var restaurantRatings = new List<RestaurantRating>
                {
                    new RestaurantRating { Score = 5, UserId = user1.Id, RestaurantId = restaurant1.Id },
                    new RestaurantRating { Score = 5, UserId = user2.Id, RestaurantId = restaurant1.Id },
                    new RestaurantRating { Score = 4, UserId = user3.Id, RestaurantId = restaurant1.Id },
                    new RestaurantRating { Score = 5, UserId = user1.Id, RestaurantId = restaurant2.Id },
                    new RestaurantRating { Score = 4, UserId = user2.Id, RestaurantId = restaurant2.Id },
                    new RestaurantRating { Score = 5, UserId = user4.Id, RestaurantId = restaurant2.Id },
                    new RestaurantRating { Score = 4, UserId = user5.Id, RestaurantId = restaurant2.Id },
                    new RestaurantRating { Score = 5, UserId = user1.Id, RestaurantId = restaurant3.Id },
                    new RestaurantRating { Score = 5, UserId = user3.Id, RestaurantId = restaurant3.Id },
                    new RestaurantRating { Score = 4, UserId = user5.Id, RestaurantId = restaurant3.Id },
                    new RestaurantRating { Score = 4, UserId = user2.Id, RestaurantId = restaurant4.Id },
                    new RestaurantRating { Score = 5, UserId = user3.Id, RestaurantId = restaurant4.Id },
                    new RestaurantRating { Score = 4, UserId = user4.Id, RestaurantId = restaurant4.Id },
                    new RestaurantRating { Score = 5, UserId = user5.Id, RestaurantId = restaurant6.Id },
                    new RestaurantRating { Score = 4, UserId = user1.Id, RestaurantId = restaurant6.Id },
                    new RestaurantRating { Score = 5, UserId = user2.Id, RestaurantId = restaurant6.Id }
                };

                await context.RestaurantRating.AddRangeAsync(restaurantRatings);
                await context.SaveChangesAsync();

                // 10. Creăm Orders (comenzi finalizate din trecut)
                var orders = new List<Order>
                {
                    new Order { UserId = user1.Id, Date = DateTime.Now.AddDays(-7) },
                    new Order { UserId = user2.Id, Date = DateTime.Now.AddDays(-6) },
                    new Order { UserId = user3.Id, Date = DateTime.Now.AddDays(-5) },
                    new Order { UserId = user4.Id, Date = DateTime.Now.AddDays(-4) },
                    new Order { UserId = user5.Id, Date = DateTime.Now.AddDays(-3) },
                    new Order { UserId = user1.Id, Date = DateTime.Now.AddDays(-2) },
                    new Order { UserId = user2.Id, Date = DateTime.Now.AddDays(-1) }
                };

                await context.Orders.AddRangeAsync(orders);
                await context.SaveChangesAsync();

                // 11. Creăm OrderItems pentru comenzile de mai sus
                var orderItems = new List<OrderItem>
                {
                    // Order 1 - user1
                    new OrderItem { OrderId = orders[0].Id, ProductId = products[0].Id, Quantity = 1, Price = products[0].Price },
                    new OrderItem { OrderId = orders[0].Id, ProductId = products[2].Id, Quantity = 1, Price = products[2].Price },

                    // Order 2 - user2
                    new OrderItem { OrderId = orders[1].Id, ProductId = products[5].Id, Quantity = 2, Price = products[5].Price },
                    new OrderItem { OrderId = orders[1].Id, ProductId = products[7].Id, Quantity = 1, Price = products[7].Price },

                    // Order 3 - user3
                    new OrderItem { OrderId = orders[2].Id, ProductId = products[8].Id, Quantity = 1, Price = products[8].Price },
                    new OrderItem { OrderId = orders[2].Id, ProductId = products[11].Id, Quantity = 1, Price = products[11].Price },

                    // Order 4 - user4
                    new OrderItem { OrderId = orders[3].Id, ProductId = products[12].Id, Quantity = 2, Price = products[12].Price },
                    new OrderItem { OrderId = orders[3].Id, ProductId = products[14].Id, Quantity = 2, Price = products[14].Price },

                    // Order 5 - user5
                    new OrderItem { OrderId = orders[4].Id, ProductId = products[18].Id, Quantity = 1, Price = products[18].Price },
                    new OrderItem { OrderId = orders[4].Id, ProductId = products[21].Id, Quantity = 1, Price = products[21].Price },
                    new OrderItem { OrderId = orders[4].Id, ProductId = products[22].Id, Quantity = 1, Price = products[22].Price },

                    // Order 6 - user1
                    new OrderItem { OrderId = orders[5].Id, ProductId = products[4].Id, Quantity = 1, Price = products[4].Price },

                    // Order 7 - user2
                    new OrderItem { OrderId = orders[6].Id, ProductId = products[19].Id, Quantity = 2, Price = products[19].Price },
                    new OrderItem { OrderId = orders[6].Id, ProductId = products[21].Id, Quantity = 2, Price = products[21].Price }
                };

                await context.OrderItems.AddRangeAsync(orderItems);
                await context.SaveChangesAsync();
            }
        }
    }
}