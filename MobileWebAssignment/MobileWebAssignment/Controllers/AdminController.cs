using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MobileWebAssignment.Controllers;

public class AdminController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;

    public AdminController(DB db)
    {
        this.db = db;
    }
    public IActionResult AdminFeedback()

    {
        return View();
    }

    public IActionResult AdminAttraction()

    {
        ViewBag.AttractionTypes = db.AttractionType.ToList();
        ViewBag.Attractions = db.Attraction.Include(a => a.AttractionType);

        return View();
    }

    public IActionResult AdminDiscount()
    {
        return View();
    }

    // Manually generate next id for attraction type
    private string NextAttractionTypeId()
    {
        string max = db.AttractionType.Max(s => s.Id) ?? "AT000";
        int n = int.Parse(max[2..]);
        return (n + 1).ToString("'AT'000");
    }

    // GET: AttractionType/Insert
    public IActionResult AdminAttractionTypeCreate()
    {
        var vm = new AttractionTypeInsertVM
        {
            Id = NextAttractionTypeId(),
        };

        return View(vm);
    }

    // POST: AttractionType/Insert
    [HttpPost]
    public IActionResult AdminAttractionTypeCreate(AttractionTypeInsertVM vm)
    {
        
        if (ModelState.IsValid)
        {
            db.AttractionType.Add(new()
            {
                Id = vm.Id,
                Name = vm.Name,
            });
            db.SaveChanges();

            TempData["Info"] = "Attraction Type inserted.";
            return RedirectToAction("/Admin/AdminAttraction");
        }

        return View();
    }


    

}
