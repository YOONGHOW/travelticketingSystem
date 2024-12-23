
using System.Globalization;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using MobileWebAssignment.Models;
using MobileWebAssignment.Service;

namespace MobileWebAssignment.Controllers
{
    public class ClientController : Controller
    {

        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Helper hp;

        public ClientController(DB db, IWebHostEnvironment en, Helper hp, IHttpContextAccessor _httpContextAccessor)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
            this._httpContextAccessor = _httpContextAccessor;
        }


        // GET: Home/Index
        public IActionResult Index()

        {
            return View();
        }

        public IActionResult Homepage()
        {
            return View();
        }

        //============================================ Account Maintenance =========================================================

        // Generate ID for register account
        private string NextRegisterId()
        {
            string max = db.User.Max(s =>  s.Id) ?? "U000";
            int n = int.Parse(max[1..]);
            return (n + 1).ToString("'U'000");
        }

        // GET: Client/CheckEmail
        public bool CheckEmail(string email)
        {
            return !db.User.Any(u => u.Email == email);
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
            if (db.User.Any(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Duplicated Email.");
            }

            //check photo
            if (ModelState.IsValid("Photo"))
            {
                var e = hp.ValidatePhoto(vm.Photo);
                if (e != "") ModelState.AddModelError("Photo", e);
            }

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
                    PhotoURL = hp.SavePhoto(vm.Photo, "User")
                });
                db.SaveChanges();
                TempData["Info"] = "Register successfully. Please login.";
                return RedirectToAction("Login");
            }

            return View(vm);
        }

        //GET : Client/Login
        public IActionResult Login()
        {
            return View();
        }

        //POST : Client/Login
        [HttpPost]
        public IActionResult Login(LoginVm vm, string? returnURL)
        {
            if (string.IsNullOrEmpty(vm.Email) )
            {
                ModelState.AddModelError("", "Email are required.");
                return View(vm);
            }
            
            if (string.IsNullOrEmpty(vm.PasswordCurrent))
            {
                ModelState.AddModelError("", "Password are required.");
                return View(vm);
            }

            var u = db.User.SingleOrDefault(user => user.Email == vm.Email);

            if(u.Freeze == true)
            {
                ModelState.AddModelError("", "Account already block by Admin.");
                return View(vm);
            }

            if (u == null || string.IsNullOrEmpty(u.Password) || !hp.VerifyPassword(u.Password, vm.PasswordCurrent))
            {
                ModelState.AddModelError("", "Login credentials not matched.");
                return View(vm);
            }

            // Successful login
            TempData["Info"] = "Login successfully.";

            var role = (u is Admin) ? "Admin" : "Member";
            hp.SignIn(u.Email, role);

            if (!string.IsNullOrEmpty(returnURL))
            {
                return Redirect(returnURL);
            }

            if(role == "Admin")
            {
                return RedirectToAction("AdminAttraction", "Admin");

            }
            else
            {
                return RedirectToAction("Homepage", "Client");
            }

        }

        // GET : Client/Logout
        public IActionResult Logout()
        {
            TempData["Info"] = "Logout Successful.";

            hp.SignOut();

            return RedirectToAction("Login", "Client");
        }

        //GET : Client/UpdateProfile
        [Authorize(Roles = "Member")]
        public IActionResult UpdateProfile()
        {
            var email = User.Identity!.Name; // Get the logged-in user's email
            var m = db.Members.SingleOrDefault(member => member.Email == email); // Retrieve member details

            var photo = $"/User/{m.PhotoURL}";

            if (m == null) return RedirectToAction("Homepage", "Client");

            var birthDate = Helper.ConvertIcToBirthDate(m.IC);

            var vm = new UpdateProfileVm
            {
                Name = m.Name,
                Email = m.Email,
                PhoneNumber = m.PhoneNumber,
                Gender = m.Gender,
                IC = m.IC,
                BirthDate = birthDate,
                PhotoURL = photo,
            };

            return View(vm); 
        }

        //POST : Client/UpdateProfile
        [HttpPost]
        public IActionResult UpdateProfile(UpdateProfileVm vm)
        {

            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);

            if (user == null)
            {
                return RedirectToAction("Homepage", "Client"); // Redirect if user not found
            }

            //check photo
            if (ModelState.IsValid("Photo"))
            {
                var e = hp.ValidatePhoto(vm.Photo);
                if (e != "") ModelState.AddModelError("Photo", e);
            }


            // Validate other fields (e.g., name)
            if (string.IsNullOrWhiteSpace(vm.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
            }

            // Additional validation for other fields can go here

            if (ModelState.IsValid)
            {
                // Update user details
                user.Name = vm.Name.Trim();
                user.PhoneNumber = vm.PhoneNumber?.Trim(); // Handle optional fields
                user.Gender = vm.Gender;
                user.IC = vm.IC;

                // Update profile photo if a new one is uploaded
                
                if (vm.Photo != null)
                {
                    hp.DeletePhoto(user.PhotoURL, "User");
                    user.PhotoURL = hp.SavePhoto(vm.Photo, "User");
                }

                // Save changes to the database
                db.SaveChanges();

                TempData["Info"] = "Profile updated successfully.";
                return RedirectToAction("Homepage", "Client");
            }

            return View(vm);
        }

        //GET Client/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //POST Client/ChangePassword
        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(ChangePassword vm)
        {
            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);


            if (user == null)
            {
                return RedirectToAction("Homepage", "Client"); // Redirect if user not found
            }

            if (!hp.VerifyPassword(user.Password, vm.CurrentPassword))
            {
                ModelState.AddModelError("Current", "Current Password Incorrect.");
            }


            if (ModelState.IsValid)
            {

                user.Password = hp.HashPassword(vm.NewPassword);
                db.SaveChanges();

                TempData["Info"] = "Password updated.";
                return RedirectToAction("Homepage","Client");

            }


            return View(vm);
        }

        //GET Client/ResetPassword
        public IActionResult ResetPassword()
        {
            return View();
        }

        //POST Client/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPassword vm)
        {
            if (vm.RecaptchaToken == null)
            {
                ModelState.AddModelError("", "reCAPTCHA validation failed.");
                return View(vm);
            }
            else if (!await RecaptchaService.verifyRecaptchaV2(vm.RecaptchaToken, "6LcsrqMqAAAAAKF7Sxh6S-SBwDC3czdQMo00XPhj"))
            {
                ModelState.AddModelError("", "Token sent fail");
                return View(vm);
            }

            var u = db.User.SingleOrDefault(user => user.Email == vm.Email);

            if (u == null)
            {
                ModelState.AddModelError("Email", "Email not found.");
            }

            if (ModelState.IsValid) 
            {
                string password = hp.RandomPassword();

                u!.Password = hp.HashPassword(password);
                db.SaveChanges();

                // Send reset password email
                SendResetPasswordEmail(u, password);

                TempData["Info"] = $"Password reset. Check your email.";
                return RedirectToAction("Login","Client");
            }


            return View();
        }

        //Send password to email
        private void SendResetPasswordEmail(User u, string password)
        {
            var mail = new MailMessage();
            mail.To.Add(new MailAddress(u.Email, u.Name));
            mail.Subject = "Reset Password";
            mail.IsBodyHtml = true;


            var url = Url.Action("Login", "Client", null, "https");


            var path = u switch
            {
                Admin => Path.Combine(en.WebRootPath, "images", "reset-password.png"),
                Member m => Path.Combine(en.WebRootPath, "User", m.PhotoURL),
                _ => "",
            };

            var att = new Attachment(path);
            mail.Attachments.Add(att);
            att.ContentId = "photo";

            mail.Body = $@"
            <img src='cid:photo' style='width: 200px; height: 200px;
                                        border: 1px solid #333'>
            <p>Dear {u.Name},<p>
            <p>Your password has been reset to:</p>
            <h1 style='color: red'>{password}</h1>
            <p>
                Please <a href='{url}'>login</a>
                with your new password.
            </p>
            <p>From, 🐱 Super Admin</p>
        ";

            hp.SendEmail(mail);
        }
        //============================================ Account Maintenance End =========================================================

        public IActionResult ClientAttraction()
        {
            ViewBag.AttractionTypes = db.AttractionType.ToList();

            var attractions = db.Attraction.Include(a => a.AttractionType).ToList();
            ViewBag.Attractions = attractions;

            var attractFeedback = new List<AttractFeedback>(); 

            foreach(var a in attractions)
            {
                attractFeedback.Add(new AttractFeedback
                {
                    attraction = a,
                    feedbacks = db.Feedback.Where(f => f.AttractionId == a.Id).ToList(),
                });
            }

            return View(attractFeedback);
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
        //------------------------------------------ Cart start ----------------------------------------------
        //Auto generate id
        private string NextCartId()
        {
            string max = db.Cart.Max(s => s.Id) ?? "C0000";
            int n = int.Parse(max[2..]);
            return (n + 1).ToString("'C'0000");
        }

        [HttpPost]
        public IActionResult AddToCart(string ticketId, int quantity)
        {
            var userId = "U001";
            //var userId = User.Identity!.Name;
            //if (userId == null)
            //{
            //    TempData["Error"] = "You must log in to add items to the cart.";
            //    return RedirectToAction("Login");
            //}

            var ticket = db.Ticket.SingleOrDefault(t => t.Id == ticketId);
            if (ticket == null || ticket.stockQty < quantity)
            {
                TempData["Error"] = "Invalid ticket or insufficient stock.";
                return RedirectToAction("ClientAttractionDetail", new { AttractionId = ticket?.AttractionId });
            }

            var existingCart = db.Cart.SingleOrDefault(c => c.UserId == userId && c.TicketId == ticketId);
            if (existingCart != null)
            {
                existingCart.Quantity += quantity;
            }
            else
            {
                db.Cart.Add(new Cart
                {
                    Id = NextCartId(),
                    UserId = userId,
                    TicketId = ticketId,
                    Quantity = quantity,
                });
            }

            db.SaveChanges();
            TempData["Info"] = "Item added to cart successfully.";
            return RedirectToAction("ClientAttractionDetail", new { AttractionId = ticket.AttractionId });
        }



      //  [Authorize(Roles = "Member")] 
        public IActionResult ClientCart()
        {
            //var userId = User.Identity!.Name;
            var userId = "U001";
            //if (userId == null)
            //{
            //    TempData["Error"] = "You must log in to view your cart.";
            //    return RedirectToAction("Login");
            //}

            var cartItems = db.Cart
                              .Include(c => c.Ticket)
                              .Where(c => c.UserId == userId)
                              .Select(c => new
                              {
                                  c.Id,
                                  TicketName = c.Ticket.ticketName,
                                  Quantity = c.Quantity,
                                  Price = c.Ticket.ticketPrice,
                                  TotalPrice = c.Quantity * c.Ticket.ticketPrice,
                                  StockAvailable = c.Ticket.stockQty
                              })
                              .ToList();

            ViewBag.CartItems = cartItems;
            ViewBag.TotalPrice = cartItems.Sum(c => c.TotalPrice);

            return View();
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
            List<Ticket> ticketList = new List<Ticket>();
            Ticket ticket = new Ticket { };


            return View();
        }
        public IActionResult ClientPaymentHIS()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            session.SetString("UserName", "U0001");
            var trySession = session.GetString("UserName");
            {
                var m = db.PurchaseItem
                    .Include(re => re.Ticket)
                    .Include(re => re.Purchase)
                    .Where(re=>re.Purchase.UserId==trySession).ToList();
                   

                return View(m);
            }
            
        }
    }
}
