using Microsoft.AspNetCore.Mvc;

namespace MobileWebAssignment.Controllers
{
    public class ClientController : Controller
    {
        // GET: Home/Index
        public IActionResult Index()

        {
            return View();
        }

        //register for a account
        public IActionResult RegisterAccount()
        {
            return View();
        }

        public IActionResult login()
        {
            return View();
        }

        public IActionResult Homepage()
        {
            return View();
        }

        public IActionResult ClientAttraction()
        {
            return View();
        }

        public IActionResult ClientAttractionDetail()
        {
            return View();
        }


    }
}
