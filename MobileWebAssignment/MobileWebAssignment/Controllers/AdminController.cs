using Microsoft.AspNetCore.Mvc;

namespace MobileWebAssignment.Controllers
{
    public class AdminController : Controller
    {
        //[Route("Admin/AdminFeedBack")]
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
