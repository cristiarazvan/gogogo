using Microsoft.AspNetCore.Mvc;

public class CategoriesController : Controller 
{
    public IActionResult Index()
    {
        var categories = new List<string> 
        { 
            "Mâncare", 
            "Supermarket", 
            "Farmacie", 
            "Băuturi", 
            "Gustări", 
            "Cadouri" 
        };

        return View(categories);
    }
}
