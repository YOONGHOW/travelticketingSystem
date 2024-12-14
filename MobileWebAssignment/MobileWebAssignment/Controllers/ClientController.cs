using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MobileWebAssignment.Controllers
{
    public class ClientController : Controller
    {

        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public ClientController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

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
            ViewBag.AttractionTypes = db.AttractionType.ToList();
            ViewBag.Attractions = db.Attraction.Include(a => a.AttractionType);
            return View();
        }

        public IActionResult ClientAttractionDetail()
        {
            return View();
        }

        public IActionResult ClientPayment()
        {
            return View();
        }

    }
}
