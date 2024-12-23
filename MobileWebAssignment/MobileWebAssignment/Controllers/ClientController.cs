using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileWebAssignment.Models;

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

        //GET: AttractionDetail
        public IActionResult ClientAttractionDetail(string? AttractionId)
        {

            var a = db.Attraction.Find(AttractionId);
            var feedbacks = db.Feedback.Where(f => f.AttractionId == AttractionId).ToList();

            if (a == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            ViewBag.Feedbacks = new List<FeedbackInsertVM>();
            foreach (var f in feedbacks)
            {
                ViewBag.Feedbacks.Add(new FeedbackInsertVM
                {
                    Id = f.Id,
                    Comment = f.Comment,
                    Rating = f.Rating,
                    SubmitDate = f.SubmitDate,
                    AttractionId = f.AttractionId,
                    commentDetail = hp.ConvertComment(f.Comment),
                });
            }

            var tickets = db.Ticket.Where(t => t.AttractionId == AttractionId).ToList();
            ViewBag.Tickets = tickets.Select(t => new TicketVM
            {
                Id = t.Id,
                ticketName = t.ticketName,
                stockQty = t.stockQty,
                ticketPrice = t.ticketPrice,
                ticketStatus = t.ticketStatus,
                ticketDetails = t.ticketDetails,
                ticketType = t.ticketType,
                AttractionId = t.AttractionId,             

            }).ToList();

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


        public IActionResult ClientFeedback(string? userId)
        {
            var feedbacks = db.Feedback.Include(a => a.Attraction).Include(u => u.User).Where(f => f.UserId == userId).ToList();

            if(feedbacks == null)
            {
                return RedirectToAction("HomePage");
            }

            ViewBag.attractions = db.Attraction.ToList();

            var vm = new List<FeedbackInsertVM>();
            foreach (var f in feedbacks)
            {
                vm.Add(new FeedbackInsertVM
                {
                    Id = f.Id,
                    Comment = f.Comment,
                    Rating = f.Rating,
                    SubmitDate = f.SubmitDate,
                    AttractionId = f.AttractionId,
                    commentDetail = hp.ConvertComment(f.Comment),
                });
            }


            return View(vm);
        }

        [HttpPost]
        public IActionResult ClientFeedback(List<FeedbackInsertVM> vm, string feedbackId, string userId)
        {
            var f = db.Feedback.Find(feedbackId);

            if (f != null)
            {
                db.Feedback.Remove(f);
                db.SaveChanges();
                TempData["Info"] = "Record Deleted.";
                return RedirectToAction("ClientFeedback", new { userId = userId });
            }

            return View(vm);
        }

        public IActionResult ClientFeedbackUpdate(string? Id)
        {
            var f = db.Feedback.Find(Id);
            if (f == null)
            {
                return RedirectToAction("ClientFeedback");
            }

            ViewBag.Attraction = db.Attraction.Find(f.AttractionId);

            if (ViewBag.Attraction == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            Comment cd = hp.ConvertComment(f.Comment);

            var vm = new FeedbackInsertVM
            {
                Id = f.Id,
                AttractionId = f.AttractionId,
                UserId = f.UserId,
                SubmitDate = f.SubmitDate,
                Rating = f.Rating,
                Comment = f.Comment,
                commentDetail = cd,
                Partner = cd.Partner.Trim(),
                Title = cd.Title.Trim(),
                Review = cd.Review.Trim(),
                Reason = cd.Reason.Trim(),
            };

            return View(vm);
        }


        [HttpPost]
        public IActionResult ClientFeedbackUpdate(FeedbackInsertVM vm)
        {
            var f = db.Feedback.Find(vm.Id);

            if (f == null)
            {
                return RedirectToAction("ClientFeedback");
            }

            if (ModelState.IsValid)
            {
                string comment = vm.Title + " | " + vm.Reason + " | " + vm.Partner + " | " + vm.Review;

                f.Rating = vm.Rating;
                f.Comment = comment.Trim();
                db.SaveChanges();

                TempData["Info"] = "Review Updated.";
                return RedirectToAction("ClientFeedback", new { UserId = vm.UserId });
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
