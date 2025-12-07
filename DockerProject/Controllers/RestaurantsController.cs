using Microsoft.AspNetCore.Mvc;
using DockerProject.Models;
using System.Collections.Generic;
using System.Linq;

namespace DockerProject.Controllers;

public class RestaurantsController : Controller
{
    private static List<Restaurant> _restaurants = new List<Restaurant>
    {
        new Restaurant
        {
            Id = 1,
            Name = "Pizza Delicious",
            OwnerId = "user_popescu",
            IsApproved = true,
            Products = new List<Product>
            {
                new Product { Id = 101, Title = "Pizza Margherita", Price = 35.50, Stock = 100, RestaurantId = 1 },
                new Product { Id = 102, Title = "Cola Zero", Price = 7.00, Stock = 250, RestaurantId = 1 }
            }
        },
        new Restaurant
        {
            Id = 2,
            Name = "Burger King Mock",
            OwnerId = "user_ionescu",
            IsApproved = false,
            Products = new List<Product>
            {
                new Product { Id = 201, Title = "Whopper", Price = 25.99, Stock = 15, RestaurantId = 2 }
            }
        }
    };

    public IActionResult Index()
    {
        return View(_restaurants);
    }

    public IActionResult Details(int id)
    {
        var restaurant = _restaurants.FirstOrDefault(r => r.Id == id);

        if (restaurant == null)
        {
            return NotFound(); 
        }

        return View(restaurant);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Create(Restaurant newRestaurant)
    {
        int newId = _restaurants.Any() ? _restaurants.Max(r => r.Id) + 1 : 1;
        newRestaurant.Id = newId;
        
        newRestaurant.Products = new List<Product>();

        _restaurants.Add(newRestaurant);

        return RedirectToAction("Index");
    }
}
