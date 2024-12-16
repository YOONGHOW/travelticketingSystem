using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MobileWebAssignment.Controllers
{
    public class ClientController : Controller
    {

        private readonly DB db;
        private readonly Helper hp;

        public ClientController(DB db,Helper hp)
        {
            this.db = db;
            this.hp = hp;
        }

        // GET: Home/Index
        public IActionResult Index()

        {
            return View();
        }

        //testing purpose: 
        [Authorize]
        public IActionResult testlogin()
        {
            return View();
        }

        // Generate ID for register account
        private string NextRegisterId()
        {
            string max = db.User.Max(s =>  s.Id) ?? "U000";
            int n = int.Parse(max[1..]);
            return (n + 1).ToString("'U'000");
        }

        //register for a account //GET
        public IActionResult RegisterAccount()
        {
            return View();
        }

        //register for a account //POST 
        [HttpPost]
        public IActionResult RegisterAccount(RegisterVM vm)
        {

            if (ModelState.IsValid)
            {
                db.Members.Add(new()
                {
                    Id = NextRegisterId(),
                    Name = vm.Name,
                    Email = vm.Email,
                    Password = hp.HashPassword(vm.Password),
                    IC = vm.IC,
                    PhoneNumber = vm.PhoneNumber,
                    Gender = vm.Gender,
                    Freeze = false,
                    PhotoURL = "/images/default_profile.png" //testing

                });
                db.SaveChanges();
                TempData["Info"] = "Register successfully. Please login.";
                return RedirectToAction("Login");
            }

            return View(vm);
        }

        //POST : Account/Login
        public IActionResult Login()
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
