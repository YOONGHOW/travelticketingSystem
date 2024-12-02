using Microsoft.AspNetCore.Mvc;

namespace MobileWebAssignment.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
