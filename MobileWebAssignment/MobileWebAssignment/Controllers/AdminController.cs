using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MobileWebAssignment.Controllers
{
    public class AdminController : Controller
    {
        private readonly DB db;

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
        string max = db.AttractionType.Max(s => s.Id) ?? "AT0000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'AT'0000");
    }

    }
}
