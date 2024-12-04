using Microsoft.AspNetCore.Mvc;

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
            return View();
        }

        public IActionResult AdminDiscount()
        {
            return View();
        }
    }
}
