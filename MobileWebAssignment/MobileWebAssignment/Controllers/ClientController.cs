using Microsoft.AspNetCore.Mvc;

namespace MobileWebAssignment.Controllers
{
    public class ClientController : Controller
    {

        private readonly DB db;

        public ClientController(DB db)
        {
            this.db = db;
        }

        // GET: Home/Index
        public IActionResult Index()

        {
            return View();
        }

        //register for a account //GET
        public IActionResult RegisterAccount()
        {
            return View();
        }

        //register for a account //POST //I DO UNTIL HERE!!!!
        [HttpPost]
        public IActionResult RegisterAccount(RegisterVM vm)
        {

            if (ModelState.IsValid)
            {
                db.Members.Add(new() 
                {
                    Id = "U001",
                    Name = vm.Name,
                    Email = vm.Email,
                    Password = vm.Password,
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

        public IActionResult ClientPayment()
        {
            return View();
        }

    }
}
