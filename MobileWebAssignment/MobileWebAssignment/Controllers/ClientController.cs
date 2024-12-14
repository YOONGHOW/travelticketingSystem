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

        //GET: AttractionDetail
        public IActionResult ClientAttractionDetail(string? AttractionId)
        {

            var a = db.Attraction.Find(AttractionId);
            ViewBag.Feedbacks = db.Feedback.Where(f => f.AttractionId == AttractionId).ToList();

            if (a == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            var vm = new AttractionUpdateVM
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Location = a.Location,
                OperatingHours = a.OperatingHours,
                ImagePath = a.ImagePath,
                AttractionTypeId = a.AttractionTypeId,
                operatingTimes = hp.ConvertOperatingTimes(a.OperatingHours),
            };

            return View(vm);
        }

        //------------------------------------------ FeedBack start ----------------------------------------------

        // Manually generate next id for feedback
        private string NextFeedbackId()
        {
            string max = db.Feedback.Max(s => s.Id) ?? "F000";
            int n = int.Parse(max[1..]);
            return (n + 1).ToString("'F'000");
        }

        //GET: Feedback/Insert
        public IActionResult ClientFeedbackForm(string attractionId)
        {
            ViewBag.Attraction = db.Attraction.Find(attractionId);

            if (ViewBag.Attraction == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            var vm = new FeedbackInsertVM
            {
                Id = NextFeedbackId(),
                AttractionId = attractionId,
                UserId = "U001",
            };

            return View(vm);
        }

        //POST: Feedback/Insert
        [HttpPost]
        public IActionResult ClientFeedbackForm(FeedbackInsertVM vm)
        {
            ViewBag.Attraction = db.Attraction.Find(vm.AttractionId);

            if (ViewBag.Attraction == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            if (ModelState.IsValid("Title") && vm.Title == null) 
            {
                ModelState.AddModelError("Title", "Please enter your title.");
            }

            if (ModelState.IsValid("Partner") && vm.Partner == null)
            {
                ModelState.AddModelError("Partner", "Please choose your partner.");
            }

            if (ModelState.IsValid("Reason") && vm.Reason.Equals("none"))
            {
                ModelState.AddModelError("Reason", "Please enter your comment." + vm.Rating);
            }

            if (ModelState.IsValid("Review") && vm.Review == null)
            {
                ModelState.AddModelError("Review", "Please enter your review.");
            }


            if (ModelState.IsValid)
            {
                string comment = vm.Title + " | " + vm.Reason + " | " + vm.Partner + " | " + vm.Review;
                db.Feedback.Add(new()
                {
                    Id= vm.Id,
                    Rating= vm.Rating,
                    Comment= comment,
                    SubmitDate= DateTime.Now,
                    AttractionId= vm.AttractionId,
                    UserId= vm.UserId,
                });
                db.SaveChanges();

                TempData["Info"] = "Your feedback is submitted.";
                return RedirectToAction("ClientAttractionDetail", new { AttractionId = vm.AttractionId });
            }

            return View(vm);
        }

//------------------------------------------ FeedBack end ----------------------------------------------


        public IActionResult ClientPayment()
        {
            return View();
        }

    }
}
