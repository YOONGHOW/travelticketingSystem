
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
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
            string max = db.User.Max(s => s.Id) ?? "U000";
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

            string photoPath = null;

            // Handle Base64 photo (Webcam)
            if (!string.IsNullOrEmpty(vm.PhotoBase64))
            {
                var base64Data = Regex.Match(vm.PhotoBase64, @"data:image/(?<type>.+?);base64,(?<data>.+)").Groups["data"].Value;
                var bytes = Convert.FromBase64String(base64Data);

                string fileName = $"{Guid.NewGuid():N}.jpg";
                string filePath = Path.Combine("wwwroot/User", fileName);
                System.IO.File.WriteAllBytes(filePath, bytes);

                photoPath = fileName;
            }
            // Handle uploaded file
            else if (vm.Photo != null && vm.Photo.Length > 0)
            {
                var photoError = hp.ValidatePhoto(vm.Photo);
                if (!string.IsNullOrEmpty(photoError))
                {
                    ModelState.AddModelError("Photo", photoError);
                }
                else
                {
                    photoPath = hp.SavePhoto(vm.Photo, "User");
                }
            }

            if (string.IsNullOrEmpty(photoPath))
            {
                ModelState.AddModelError("Photo", "Please provide a profile photo by uploading or capturing one.");
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
                    PhotoURL = photoPath,
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
        public async Task<IActionResult> Login(LoginVm vm, string? returnURL)
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

            if (string.IsNullOrEmpty(vm.Email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View(vm);
            }

            if (string.IsNullOrEmpty(vm.PasswordCurrent))
            {
                ModelState.AddModelError("", "Password is required.");
                return View(vm);
            }

            // Retrieve the failed attempts cookie
            var failedAttemptsCookie = Request.Cookies["FailedLoginAttempts"];
            int failedAttempts = string.IsNullOrEmpty(failedAttemptsCookie) ? 0 : int.Parse(failedAttemptsCookie);

            // Check if the user is temporarily blocked
            if (failedAttempts >= 3)
            {
                ModelState.AddModelError("", "Your account is temporarily blocked. Please try again after some time.");
                return View(vm);
            }

            // Find user in the database
            var user = db.User.SingleOrDefault(u => u.Email == vm.Email);

            if (user == null || string.IsNullOrEmpty(user.Password) || !hp.VerifyPassword(user.Password, vm.PasswordCurrent))
            {
                // Increment the failed attempts
                failedAttempts++;

                // Update the cookie
                CookieOptions options = new()
                {
                    Expires = DateTime.Now.AddSeconds(30), // Block for 30 seconds
                    HttpOnly = true,
                    IsEssential = true
                };

                Response.Cookies.Append("FailedLoginAttempts", failedAttempts.ToString(), options);

                if (failedAttempts >= 3)
                {
                    ModelState.AddModelError("", "Your account is temporarily blocked due to multiple failed login attempts.");
                }
                else
                {
                    ModelState.AddModelError("", "Login credentials not matched.");
                }

                return View(vm);
            }

            // Reset the failed attempts on successful login
            Response.Cookies.Delete("FailedLoginAttempts");

            // Check if the account is frozen
            if (user.Freeze)
            {
                ModelState.AddModelError("", "Account is blocked by Admin.");
                return View(vm);
            }

            //Sucessfully login
            TempData["Info"] = "Login successfully.";

            var role = (user is Admin) ? "Admin" : "Member";
            hp.SignIn(user.Email, role);

            if (!string.IsNullOrEmpty(returnURL))
            {
                return Redirect(returnURL);
            }

            if (role == "Admin")
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
                return RedirectToAction("Homepage", "Client");

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
                return RedirectToAction("Login", "Client");
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

            foreach (var a in attractions)
            {
                attractFeedback.Add(new AttractFeedback
                {
                    attraction = a,
                    feedbacks = db.Feedback.Where(f => f.AttractionId == a.Id).ToList(),
                    tickets = db.Ticket.Where(t => t.AttractionId == a.Id).ToList(),
                });
            }



            foreach (var a in attractFeedback)
            {
                if (hp.SplitImagePath(a.attraction.ImagePath).Count > 0)
                    a.attraction.ImagePath = hp.SplitImagePath(a.attraction.ImagePath)[0];
            }

            return View(attractFeedback);
        }

        [HttpPost]
        public IActionResult ClientAttraction(string? name, string? category, string? sort)
        {
            ViewBag.AttractionTypes = db.AttractionType.ToList();

            // Searching for attraction
            name = name?.Trim() ?? "";
            category = category == "all" ? "" : category?.Trim();

            var attractions = db.Attraction.Include(a => a.AttractionType)
                                           .Where(a => a.Name.Contains(name))
                                           .Where(a => a.AttractionTypeId.Contains(category))
                                           .ToList();

            ViewBag.Attractions = attractions;

            var attractFeedback = new List<AttractFeedback>();

            foreach (var a in attractions)
            {
                attractFeedback.Add(new AttractFeedback
                {
                    attraction = a,
                    feedbacks = db.Feedback.Where(f => f.AttractionId == a.Id).ToList(),
                    tickets = db.Ticket.Where(t => t.AttractionId == a.Id).ToList(),
                });
            }

            foreach (var a in attractFeedback)
            {
                if (hp.SplitImagePath(a.attraction.ImagePath).Count > 0)
                    a.attraction.ImagePath = hp.SplitImagePath(a.attraction.ImagePath)[0];
            }

            //get the minimum ticket price of each atttraction if have
            foreach(var a in attractFeedback)
            {
                if(a.tickets.Count > 0)
                {
                    a.ticketPrice = hp.GetMinTicketPrice(a.tickets);
                }
            }

            Func<AttractFeedback, object> attraction = "ticketPrice" switch
            {
                "ticketPrice" => t => t.ticketPrice,
                _ => t => t.ticketPrice,
            };

            string Asort = sort == "High To Low" ? "des" : "asc";


            //perform price sorting when needed
            var at = Asort == "des" ?
                    attractFeedback.OrderByDescending(attraction) :
                    attractFeedback.OrderBy(attraction);

            attractFeedback = at.ToList();


            return PartialView("_ClientAttraction", attractFeedback);
        }

        //GET: AttractionDetail
        public IActionResult ClientAttractionDetail(string? AttractionId)
        {
            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);

            ViewBag.User = user;

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
                Photo = new UpdateImageSet(),
            };

            vm.Photo.imagePaths = hp.SplitImagePath(a.ImagePath);

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
        public IActionResult AddMultipleToCart(List<CartItem> tickets)
        {
            var userId = "U001"; // Replace with actual user ID retrieval logic.
            string attractionId = null; // Initialize AttractionId.

            foreach (var ticket in tickets)
            {
                if (ticket.Quantity <= 0) continue; // Skip tickets with zero or negative quantities.

                var dbTicket = db.Ticket.SingleOrDefault(t => t.Id == ticket.TicketId);
                if (dbTicket == null || dbTicket.stockQty < ticket.Quantity)
                {
                    TempData["Error"] = $"Invalid ticket or insufficient stock for ticket {ticket.TicketId}.";
                    continue;
                }

                attractionId = dbTicket.AttractionId; // Capture the AttractionId from a valid ticket.

                var existingCart = db.Cart.SingleOrDefault(c => c.UserId == userId && c.TicketId == ticket.TicketId);
                if (existingCart != null)
                {
                    existingCart.Quantity += ticket.Quantity;
                }
                else
                {
                    db.Cart.Add(new Cart
                    {
                        Id = NextCartId(),
                        UserId = userId,
                        TicketId = ticket.TicketId,
                        Quantity = ticket.Quantity,
                    });
                }
            }

            db.SaveChanges();

            TempData["Info"] = "Selected items added to cart successfully.";
            return RedirectToAction("ClientAttractionDetail", new { AttractionId = attractionId });
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
        [Authorize(Roles = "Member")]
        public IActionResult ClientFeedbackForm(string attractionId)
        {
            ViewBag.Attraction = db.Attraction.Find(attractionId);

            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);

            if (ViewBag.Attraction == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            var vm = new FeedbackInsertVM
            {
                Id = NextFeedbackId(),
                AttractionId = attractionId,
            };

            if (user != null)
            {
                vm.UserId = user.Id;
            }
            else
            {
                vm.UserId = "none";
            }

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
                    Id = vm.Id,
                    Rating = vm.Rating,
                    Comment = comment,
                    SubmitDate = DateTime.Now,
                    AttractionId = vm.AttractionId,
                    UserId = vm.UserId,
                });
                db.SaveChanges();

                TempData["Info"] = "Your feedback is submitted.";
                return RedirectToAction("ClientAttractionDetail", new { AttractionId = vm.AttractionId });
            }

            return View(vm);
        }

        [Authorize(Roles = "Member")]
        public IActionResult ClientFeedback()
        {
            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);

            var feedbacks = db.Feedback.Include(a => a.Attraction).Include(u => u.User).Where(f => f.UserId == user.Id).ToList();

            if (feedbacks == null)
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

        //add cart
        public void UpdateCart(string productId, int quantity)
        {
            var cart = hp.GetCart();
            if (quantity >= 1 && quantity <= 1000)
            {
                cart[productId] = quantity;
            }
            else
            {
                cart.Remove(productId);
            }
            hp.SetCart(cart);

        }

        //GET client/clienPayment
        [Authorize(Roles = "Member")]
        public IActionResult ClientPayment()
        {
            UpdateCart("T002", 9);
            UpdateCart("T001", 9);
            var cart = hp.GetCart();

            // Check if the cart is valid and contains data
            if (cart == null || !cart.Any())
            {
                ViewBag.CartItems = new List<CartPVM>(); // Assign an empty list to avoid null issues
            }
            else
            {
                var m = db.Ticket
                    .Include(t => t.Attraction)
                    .Where(t => cart.Keys.Contains(t.Id))
                    .Select(p => new CartPVM
                    {
                        Ticket = p,
                        Quantit = cart[p.Id],
                        Subtotal = p.ticketPrice * cart[p.Id],
                        imagepath = p.Attraction.ImagePath,
                    }).ToList();

                ViewBag.CartItems = m;
            }

            return View();

        }


        [HttpPost]

        [Authorize(Roles = "Member")]
        public IActionResult ClientPaymentHIS()

        {
            if (ModelState.IsValid)
            {
                // Process the payment (e.g., save to the database)
                hp.SetUserID("U0001");
                if (hp.GetUserID == null)
                {
                    return View();
                }
                CheckOut();
            }
            var cart = hp.GetCart();

            // Check if the cart is valid and contains data
            if (cart == null || !cart.Any())
            {
                ViewBag.CartItems = new List<CartPVM>(); // Assign an empty list to avoid null issues
            }
            else
            {
                var m = db.Ticket
                    .Include(t => t.Attraction)
                    .Where(t => cart.Keys.Contains(t.Id))
                    .Select(p => new CartPVM
                    {
                        Ticket = p,
                        Quantit = cart[p.Id],
                        Subtotal = p.ticketPrice * cart[p.Id],
                        imagepath = p.Attraction.ImagePath,
                    }).ToList();

                ViewBag.CartItems = m;
            }
            return View();
        }

        /// ------------------------------------------------
        /// show result
        /// ------------------------------------------------
        public List<Purchase> getAllPurcahse(bool unpaid)
        {
            hp.SetUserID("U001");
            var userID = hp.GetUserID();
            var allPurchases = db.Purchase
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Ticket)
                        .ThenInclude(at => at.Attraction)
                .Include(us => us.User)
                .Where(p => p.UserId == userID && p.User is Member)
                .OrderByDescending(p => p.PaymentDateTime)
                .ToList();


            if (unpaid == false)
            {
                allPurchases = allPurchases
                .Where(p => p.Status == "S" || p.Status == "R") // Compare p.Id with Payment.PurchaseId
                 .ToList();
                return allPurchases;
            }

            allPurchases = allPurchases
           .Where(p => p.Status == "F")
           .ToList();


            return allPurchases;
        }
        public IActionResult ClientPaymentHIS(string? purchaseID, DateTime? validdate, string? Unpaid)
        {
            // Retrieve all PurchaseItem records with related data


            var allPurchases = getAllPurcahse(false);


            IEnumerable<Purchase> purchaseItems;
            //Purcahse fillter
            if (Unpaid != null)
            {
                purchaseItems = getAllPurcahse(true); // If no validdate is provided, show all purchase items(Unpaid)
            }
            else
            {
                purchaseItems = allPurchases;  // If no validdate is provided, show all purchase items(paid)
            }


            //stating VM
            var viewModel = new PurchaseViewModel();
            var member = purchaseItems.Select(p => p.User as Member).FirstOrDefault();

            if (Request.IsAjax())
            {
                viewModel = new PurchaseViewModel
                {
                    Purchases = purchaseItems,
                    PhotoURL = member?.PhotoURL,
                    PurchaseUpdate = new PurchaseUpdateVM() // Initialize or fetch as required
                };
                return PartialView("PurchaseTable/_SharePurchaseTable1", viewModel);
            }
            viewModel = new PurchaseViewModel
            {
                Purchases = purchaseItems,
                PhotoURL = member?.PhotoURL,
                PurchaseUpdate = new PurchaseUpdateVM() // Initialize or fetch as required
            };
            //return view
            return View(viewModel);

        }
        public ActionResult ClientPurchaseDetail(string purchaseID)
        {
            if (string.IsNullOrEmpty(purchaseID))
            {
                return Json(new { error = "Purchase ID cannot be null or empty." });
            }

            // Retrieve and group PurchaseItems by AttractionName and ValidDate (only the date part)
            var groupedItems = db.PurchaseItem
                .Where(pi => pi.PurchaseId == purchaseID)
                .Include(pi => pi.Ticket)
                .ThenInclude(at => at.Attraction)
                .GroupBy(pi => new
                {
                    pi.Ticket.Attraction.Name,
                    ValidDate = pi.validDate.Date  // Group by Date only, ignoring time
                })
                .Select(g => new
                {
                    attractionName = g.Key.Name,
                    validDate = g.Key.ValidDate.ToString("yyyy-MM-dd"),  // Format DateTime to string in "yyyy-MM-dd" format
                    totalQuantity = g.Sum(pi => pi.Quantity),
                    totalAmount = g.Sum(pi => pi.Quantity * pi.Ticket.ticketPrice),
                    attractionImg = g.Select(pi => pi.Ticket.Attraction.ImagePath).FirstOrDefault(),
                    ticketType = g.Select(pi => pi.Ticket.ticketName).Count(),
                    purchaseId = g.Select(pi => pi.Purchase.Id).FirstOrDefault(),
                })
                .ToList();

            if (!groupedItems.Any())
            {
                return Json(new { error = "No items found for the given Purchase ID." });
            }

            return Json(groupedItems);

        }
        public ActionResult ClientPurchaseTicket(string? purchaseId, DateTime validDate)
        {
            if (string.IsNullOrEmpty(purchaseId))
            {
                return Json(new { error = "Purchase ID cannot be null or empty." });
            }

            // Retrieve all the PurchaseItems related to the given PurchaseID
            var purchaseItems = db.PurchaseItem
                .Where(pi => pi.PurchaseId == purchaseId && pi.validDate.Date == validDate.Date)
                .Include(pi => pi.Ticket)
                .Select(pi => new
                {
                    purchaseItemId = pi.Id,
                    ticketID = pi.TicketId,
                    quantity = pi.Quantity,
                    validDate = pi.validDate.ToString("yyyy/MM/dd"),
                    ticketName = pi.Ticket.ticketName,
                    attractionName = pi.Ticket.Attraction.Name,
                    attractionImg = pi.Ticket.Attraction.ImagePath,
                    amount = pi.Quantity * pi.Ticket.ticketPrice,
                    status = pi.validDate.Date >= DateTime.Now.Date // Compare the date part (ignores time)
            ? "Valid"
            : "Invalid",
                    purcahseid = pi.Purchase.Id,
                    ticketPrice = pi.Ticket.ticketPrice,
                })
                .ToList();

            if (!purchaseItems.Any())
            {
                return Json(new { error = "No items found for the given Purchase ID." });
            }
            return Json(purchaseItems);
        }

        public IActionResult ClientPurchaseUpdate(string ticketID, string purcahseItemID)
        {
            if (string.IsNullOrEmpty(purcahseItemID) && string.IsNullOrEmpty(ticketID))
            {
                return Json(new { error = "Record Not found" });
            }

            // Retrieve all the PurchaseItems related to the given PurchaseID
            var purchaseItems = db.PurchaseItem
                .Where(pi => pi.Id == purcahseItemID && pi.TicketId == ticketID)
                .Include(pi => pi.Ticket)
                .Select(pi => new
                {
                    purcahseItemID = pi.Id,
                    ticketID = pi.TicketId,
                    quantity = pi.Quantity,
                    validDate = pi.validDate.ToString("yyyy-MM-dd hh:mm tt"),
                    ticketName = pi.Ticket.ticketName,
                    ticketPrice = pi.Ticket.ticketPrice,
                    purcahseid = pi.Purchase.Id,
                })
                 .FirstOrDefault();

            if (purchaseItems == null)
            {
                return Json(new { error = "No items found for the given PurchaseItem ID." });
            }
            return Json(purchaseItems);
        }
        //Add Purchase

        private string NextPurchase_Id()
        {
            // TODO
            string max = db.Purchase.Max(t => t.Id) ?? "P0000";
            int n = int.Parse(max[1..]);

            return (n + 1).ToString("'P'0000");
        }
        private string NextPurchaseItem_Id(int count)
        {
            // TODO
            string max = db.PurchaseItem.Max(t => t.Id) ?? "PI0000";
            int n = int.Parse(max[2..]);
            return (n + count).ToString("'PI'0000");
        }

        [HttpPost]
        public void CheckOut()
        {
            var cart = hp.GetCart();

            var m = db.Ticket
               .Include(t => t.Attraction)
               .Where(t => cart.Keys.Contains(t.Id))
               .Select(p => new CartPVM
               {
                   Ticket = p,
                   Quantit = cart[p.Id],
                   Subtotal = p.ticketPrice * cart[p.Id],
               })
               .ToList();

            decimal total = m.Sum(t => t.Subtotal);
            var purchase = new Purchase
            {
                Id = NextPurchase_Id(),
                PaymentDateTime = DateTime.Now,
                Status = "F",
                Amount = total,
                UserId = hp.GetUserID(),
                PurchaseItems = new List<PurchaseItem>() // Initialize the list

            };
            var count = 1;
            foreach (var (productId, quantity) in cart)
            {
                var p = db.Ticket.Find(productId);
                if (p == null) continue;
                purchase.PurchaseItems.Add(new PurchaseItem()
                {
                    Id = NextPurchaseItem_Id(count),
                    Quantity = quantity,
                    validDate = DateTime.Now,
                    TicketId = p.Id,
                    PurchaseId = purchase.Id,
                });
                count++;
            }

            db.Purchase.Add(purchase);
            db.SaveChanges();
            hp.SetCart(cart);
        }
    }

}

