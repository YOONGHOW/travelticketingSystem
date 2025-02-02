
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Mail;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Channels;
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
            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);

            return View(user);
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

            // Retrieve the block time
            var blockTimestampCookie = Request.Cookies["BlockTimestamp"];
            DateTime blockTimestamp = string.IsNullOrEmpty(blockTimestampCookie) ? DateTime.MinValue : DateTime.Parse(blockTimestampCookie);

            // Check if the user is temporarily blocked
            if (failedAttempts >= 3)
            {
                // Check if the block time has expired
                TimeSpan blockDuration = TimeSpan.FromSeconds(30); // Block duration 30 seconds
                DateTime blockEndTime = blockTimestamp.Add(blockDuration);

                if (DateTime.Now < blockEndTime)
                {
                    TimeSpan remainingTime = blockEndTime - DateTime.Now;
                    string remainingTimeMessage = $"Your account is temporarily blocked. Please try again after {remainingTime.Seconds} seconds.";
                    ModelState.AddModelError("", remainingTimeMessage);
                    return View(vm);
                }
                else
                {
                    // Reset the block and failed attempts after the block period ends
                    Response.Cookies.Delete("FailedLoginAttempts");
                    Response.Cookies.Delete("BlockTimestamp");
                    failedAttempts = 0;
                }
            }

            // Increase the failed attempts
            failedAttempts++;

            // Find user in the database
            var user = db.User.SingleOrDefault(u => u.Email == vm.Email);

            if (user == null || string.IsNullOrEmpty(user.Password) || !hp.VerifyPassword(user.Password, vm.PasswordCurrent))
            {
                // Update the cookies
                CookieOptions options = new()
                {
                    Expires = DateTime.Now.AddSeconds(30), // Block for 30 seconds
                    HttpOnly = true,
                    IsEssential = true
                };

                Response.Cookies.Append("FailedLoginAttempts", failedAttempts.ToString(), options);

                if (failedAttempts == 3)
                {
                    // Save the block timestamp when the user is blocked
                    Response.Cookies.Append("BlockTimestamp", DateTime.Now.ToString(), options);
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
                return RedirectToAction("MemberList", "Admin");
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
        [Authorize(Roles = "Member")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //POST Client/ChangePassword
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
            foreach (var a in attractFeedback)
            {
                if (a.tickets.Count > 0)
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
        public IActionResult ClientAttractionDetail(string? attractionId)
        {
            // Retrieve the logged-in user's email
            var email = User.Identity!.Name;

            // Find the user by email
            var user = db.Members.SingleOrDefault(member => member.Email == email);

            ViewBag.User = user;

            var a = db.Attraction.Find(attractionId);
            var feedbacks = db.Feedback.Where(f => f.AttractionId == attractionId).ToList();

            if (a == null)
            {
                return RedirectToAction("ClientAttractionDetail");
            }

            if (user != null)
            {
                bool isInWishlist = db.Wish.Any(w => w.AttractionId == attractionId && w.UserId == user.Id);
                ViewBag.IsInWishlist = isInWishlist;
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
                    feedbackReplyList = db.FeedbackReply.Where(fr => fr.FeedbackId == f.Id).Where(fr => fr.Type == "Public").ToList(),
                });
            }


            var tickets = db.Ticket.Where(t => t.AttractionId == attractionId).ToList();
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

            if (user != null)
            {
                //check whether user has purchase ticket for this attraction or not
                //get all purchase record of this user
                var purchaseHIS = db.Purchase.Where(ph => ph.UserId == user.Id).ToList();

                //get all purchase item record of all the purchase record get just now  
                var purchaseItems = new List<PurchaseItemList>();

                foreach (var p in purchaseHIS)
                {
                    if (db.PurchaseItem.Any(pi => pi.PurchaseId == p.Id))
                    {
                        purchaseItems.Add(new PurchaseItemList
                        {
                            Items = db.PurchaseItem.Where(pi => pi.PurchaseId == p.Id).ToList(),
                        });
                    }
                }
                List<Ticket> ticketList = new List<Ticket>();

                //retrieve ticket for every purchase item
                foreach (var p in purchaseItems)
                {
                    foreach (var item in p.Items)
                    {
                        ticketList.Add(db.Ticket.FirstOrDefault(t => t.Id == item.TicketId));
                    }
                }

                int attractionCheck = 0;

                foreach (var t in ticketList)
                {
                    if (t.AttractionId == a.Id)
                    {
                        attractionCheck++;
                    }
                }

                if (attractionCheck > 0)
                {
                    ViewBag.ValidCheck = true;
                }
                else
                {
                    ViewBag.ValidCheck = false;
                }
            }
            else
            {
                ViewBag.ValidCheck = false;
            }


            return View(vm);
        }
        public void getUserID()
        {
            string email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            string role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(role))
            {
                var userID = db.User.Where(p => p.Email == email).Select(p => p.Id).FirstOrDefault();
                if (userID != null)
                {
                    hp.SetUserID(userID);
                }
            }
        }
        //------------------------------------------ Cart start ----------------------------------------------
        //Auto generate id
        private string NextCartId(int count)
        {
            string max = db.Cart.Max(s => s.Id) ?? "C0000";
            int n = int.Parse(max[2..]);
            return (n + count).ToString("'C'0000");
        }

        [HttpPost]
        public IActionResult AddMultipleToCart(List<CartItem> tickets)
        {
            getUserID();
            var userId = hp.GetUserID();// Replace with actual user ID retrieval logic.
            string attractionId = null; // Initialize AttractionId.
            int count = 1;
            bool itemsAdded = false;

            foreach (var ticket in tickets)
            {
                if (ticket.Quantity <= 0)
                {
                    continue;
                }

                var dbTicket = db.Ticket.SingleOrDefault(t => t.Id == ticket.TicketId);
                if (dbTicket == null || dbTicket.stockQty < ticket.Quantity)
                {
                    TempData["Error"] = $"Invalid ticket or insufficient stock for ticket {ticket.TicketId}.";
                    continue;
                }

                attractionId = dbTicket.AttractionId;

                var existingCart = db.Cart.SingleOrDefault(c => c.UserId == userId && c.TicketId == ticket.TicketId);
                if (existingCart != null)
                {
                    existingCart.Quantity += ticket.Quantity;
                }
                else
                {
                    db.Cart.Add(new Cart
                    {
                        Id = NextCartId(count),
                        UserId = userId,
                        TicketId = ticket.TicketId,
                        Quantity = ticket.Quantity,
                    });
                }
                count++;
                itemsAdded = true;
            }

            if (!itemsAdded)
            {
                TempData["Info"] = "No ticket were added to the cart.";
                return RedirectToAction("ClientAttraction");
            }

            db.SaveChanges();

            TempData["Info"] = "Selected ticket(s) added to cart successfully.";
            return RedirectToAction("ClientAttractionDetail", new { AttractionId = attractionId });
        }

        [Authorize(Roles = "Member")]
        public IActionResult ClientCart()
        {
            getUserID();
            var userId = hp.GetUserID();
            var cartItems = db.Cart
                              .Include(c => c.Ticket)
                              .ThenInclude(t => t.Attraction)
                              .Where(c => c.UserId == userId)
                              .Select(c => new
                              {
                                  c.Id,
                                  TicketId = c.Ticket.Id,
                                  TicketName = c.Ticket.ticketName,
                                  TicketType = c.Ticket.ticketType,
                                  Quantity = c.Quantity,
                                  Price = c.Ticket.ticketPrice,
                                  TotalPrice = c.Quantity * c.Ticket.ticketPrice,
                                  StockAvailable = c.Ticket.stockQty,
                                  ImagePath = c.Ticket.Attraction.ImagePath,
                                  AttractionId = c.Ticket.AttractionId
                              })
                              .ToList();

            ViewBag.CartItems = cartItems;
            ViewBag.TotalPrice = cartItems.Sum(c => c.TotalPrice);
            ViewBag.TotalCount = cartItems.Sum(c => c.Quantity);
            return View();
        }

        [HttpPost]
        public IActionResult ClientCart([FromBody] List<CartItem> ticketData)
        {


            var cartItems = new Dictionary<string, CartItem>();

            if (ticketData == null || ticketData.Count == 0)
            {
                return BadRequest("No tickets received.");
            }

            foreach (var item in ticketData)
            {
                var newCartItem = new CartItem
                {
                    TicketId = item.TicketId,
                    Quantity = item.Quantity,
                    DateOnly = item.DateOnly
                };

                // Add the newCartItem to the cartItem list
                cartItems[item.TicketId] = newCartItem;

            }

            hp.SetCart(cartItems);

            return Json(new { message = "Checkout successful!" });
        }
       
        [HttpPost]
        public IActionResult deleteCart(string TicketId)
        {
            getUserID();
            var userId = hp.GetUserID();
            var cartItem = db.Cart.SingleOrDefault(c => c.UserId == userId && c.TicketId == TicketId);

            if (cartItem != null)
            {
                db.Cart.Remove(cartItem);
                db.SaveChanges();
                TempData["Info"] = "Selected ticket is successfully removed";
                return RedirectToAction("ClientCart");
            }

            return RedirectToAction("ClientCart");
        }


        //------------------------------------------ Wishlist start ----------------------------------------------

        private string NextWishId(int count)
        {
            string max = db.Wish.Max(s => s.Id) ?? "W0000";
            int n = int.Parse(max[2..]);
            return (n + count).ToString("'W'0000");
        }

        [HttpPost]
        public IActionResult addWish(string attractionId)
        {

            getUserID();
            int count = 1;
            var userId = hp.GetUserID();// Replace with actual user ID retrieval logic.

            if (string.IsNullOrEmpty(attractionId))
            {
                TempData["Error"] = "Attraction ID is required.";
                return RedirectToAction("ClientAttractionDetail", new { AttractionId = attractionId });
            }

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User is not logged in.";
                return RedirectToAction("ClientAttractionDetail", new { AttractionId = attractionId });
            }
            var existingWish = db.Wish.SingleOrDefault(w => w.UserId == userId && w.AttractionId == attractionId);

            if (existingWish != null)
            {
                TempData["Info"] = "Attraction already in wishlist";
                return RedirectToAction("ClientAttractionDetail", new { AttractionId = attractionId });
            }

            db.Wish.Add(new Wish
            {
                Id = NextWishId(count),
                UserId = userId,
                AttractionId = attractionId,
            });


            db.SaveChanges();

            TempData["Info"] = "Attraction added to Wishlist";
            return RedirectToAction("ClientAttractionDetail", new { AttractionId = attractionId });
        }


        [Authorize(Roles = "Member")]
        public IActionResult ClientWish()
        {
            getUserID();
            var userId = hp.GetUserID();
            var wishItems = db.Wish
                              .Include(t => t.Attraction)
                              .Where(w => w.UserId == userId)
                              .Select(w => new
                              {
                                  w.Id,
                                  AttractionName = w.Attraction.Name,
                                  Description = w.Attraction.Description,
                                  ImagePath = w.Attraction.ImagePath,
                                  AttractionId = w.AttractionId,
                              })
                              .ToList();

            ViewBag.WishItems = wishItems;
            return View();
        }


        [HttpPost]
        public IActionResult deleteWish(string AttractionId)
        {
            getUserID();
            var userId = hp.GetUserID();
            var wishItem = db.Wish.SingleOrDefault(c => c.UserId == userId && c.AttractionId == AttractionId);

            if (wishItem != null)
            {
                db.Wish.Remove(wishItem);
                db.SaveChanges();
                TempData["Info"] = "The attraction has been removed from your wishlist";
                return RedirectToAction("ClientWish");
            }

            return RedirectToAction("ClientWIsh");
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
                    feedbackReplyList = db.FeedbackReply.Where(fr => fr.FeedbackId == f.Id).ToList(),
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


        [Authorize(Roles = "Member")]
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

        //=====================================================
        //Cart Management
        //=====================================================
        public void UpdateCart(CartItem cartItem)
        {
            var cart = hp.GetCart();
            if (cartItem.Quantity >= 1 && cartItem.Quantity <= 1000)
            {
                if (cart.ContainsKey(cartItem.TicketId))
                {
                    cart[cartItem.TicketId].Quantity = cartItem.Quantity;
                }
            }
            else
            {
                cart.Remove(cartItem.TicketId);
            }
            hp.SetCart(cart);

        }
        //=====================================================
        //Payment Get/Post
        //=====================================================

        //GET client/clienPayment
        [Authorize(Roles = "Member")]
        public IActionResult ClientPayment(string? purchaseID)
        {
            if (!string.IsNullOrEmpty(purchaseID))
            {
                var purchaseItem = db.PurchaseItem.
                    Where(p => p.PurchaseId == purchaseID)
                    .ToList();
                if (purchaseItem != null)
                {
                    var cartItems = new Dictionary<string, CartItem>();

                    foreach (var item in purchaseItem)
                    {
                        var newCartItem = new CartItem
                        {
                            TicketId = item.TicketId,
                            Quantity = item.Quantity,
                            PurchaseID = item.PurchaseId,
                        };

                        // Add the newCartItem to the cartItem list
                        cartItems[item.TicketId] = newCartItem;

                    }

                    hp.SetCart(cartItems);
                }
            }
            var cart = hp.GetCart();
            // Check if the cart is valid and contains data
            if (cart == null || !cart.Any())
            {
                ViewBag.CartItems = new List<CartPaymentVM>(); // Assign an empty list to avoid null issues

            }
            else
            {
                var m = db.Ticket
                    .Include(t => t.Attraction)
                    .Where(t => cart.Keys.Contains(t.Id))
                    .Select(p => new CartPaymentVM
                    {
                        Ticket = p,

                        Quantity = cart[p.Id].Quantity,

                        Subtotal = p.ticketPrice * cart[p.Id].Quantity,
                        imagepath = p.Attraction.ImagePath,
                    }).ToList();

                getUserID();
                var UserInfo = db.User
                    .Where(p => p.Id == hp.GetUserID())
                    .Select(p => p as Member)
                    .FirstOrDefault();
                ViewBag.UserInfo = UserInfo;
                ViewBag.CartItems = m;
            }

            return View();

        }

        [HttpPost]
        public IActionResult ClientPayment(PaymentVM? vm)
        {
            // Ensure user ID is retrieved
            getUserID();
            var userID = hp.GetUserID();

            // Check if the user ID is null
            if (string.IsNullOrEmpty(userID))
            {
                TempData["Message"] = "User is not logged in. Please log in to proceed with the payment.";
                return RedirectToAction("ClientCart");
            }

            // Validate the model
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Invalid payment details. Please correct the errors and try again.";
                return RedirectToAction("ClientCart?Unpaid=unpaid");
            }
            string changes = "0";
            var cart = hp.GetCart();
            var purchaseInclude = db.Purchase
                .Where(p => p.UserId == userID)
                .Select(p => p.Id)
                .ToList();

            if (cart.Any(item => purchaseInclude.Contains(item.Value.PurchaseID)))
            {
                changes = CheckOut(userID, vm, "Bank", true);
            }
            else
            {
                // Process the payment
                changes = CheckOut(userID, vm, "Bank", false);
            }
            // Handle the result of the payment
            if (int.Parse(changes) > 0)
            {
                TempData["Message"] = "Successfully made the payment.";

                return RedirectToAction("ClientPaymentHIS");
            }
            else
            {
                TempData["Message"] = "An error occurred while processing your payment. Please try again.";
                return RedirectToAction("ClientCart");
            }
        }
        //============================================
        //ID -Purchase // Purchase Item
        //============================================
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
        //============================================
        //END
        //============================================

        //============================================
        //Purchase INSERT DB
        //============================================
        public string? CheckOut(string userID, PaymentVM? vm, string? paymentType, bool exittingPurchase)
        {
            var cart = hp.GetCart();
            if (exittingPurchase && paymentType == "Bank")
            {
                var purchaseDB = db.Purchase.FirstOrDefault(p => p.Id == cart.Values.First().PurchaseID);
                var paymentDB = db.Payment.FirstOrDefault(p => p.PurchaseId == cart.Values.First().PurchaseID);
                if (purchaseDB != null && paymentDB != null)
                {
                    // Update the Status
                    purchaseDB.Status = "S";
                    paymentDB.Status = "S";
                    paymentDB.Type = "B";
                    paymentDB.PaymentDateTime = DateTime.Now;
                    purchaseDB.PaymentDateTime=DateTime.Now;
                    if (vm!=null) {
                        var getDBPromotionData = db.Promotion
                        .FirstOrDefault(p => p.Id == vm.DiscountID && p.PromoStatus == "Active");
                        if (getDBPromotionData != null)
                        {
                            var discountRate = 1 - getDBPromotionData.PriceDeduction;
                            purchaseDB.Amount *= discountRate;
                            purchaseDB.PromotionId = vm.DiscountID;
                            paymentDB.Amount *= discountRate;
                        }
                    }
                    // Save changes to the database
                    db.SaveChanges();
                    return "1";
                }
                return "0";
            }
            var m = db.Ticket
               .Include(t => t.Attraction)
               .Where(t => cart.Keys.Contains(t.Id))
               .Select(p => new CartPaymentVM
               {
                   Ticket = p,
                   Quantity = cart[p.Id].Quantity,
                   Subtotal = p.ticketPrice * cart[p.Id].Quantity,
               })
               .ToList();

            decimal total = m.Sum(t => t.Subtotal);
            string promotionID = string.Empty;
            
            if (vm!=null) {
                var getDBPromotion = db.Promotion
                    .FirstOrDefault(p => p.Id == vm.DiscountID && p.PromoStatus == "Active");
                if (getDBPromotion != null && paymentType == "Bank")
                {
                    var discountRate = 1 - getDBPromotion.PriceDeduction;
                    total *= discountRate;
                    promotionID = getDBPromotion.Id;
                }
            }

            var purchase = new Purchase
            {
                Id = NextPurchase_Id(),
                PaymentDateTime = DateTime.Now,
                Status = "F",
                Amount = total,
                UserId = userID,
                PurchaseItems = new List<PurchaseItem>() // Initialize the list
                
            };
            if (!string.IsNullOrEmpty(promotionID) && paymentType == "Bank")
            {            
                purchase.PromotionId = promotionID;
            }
            var count = 1;
            foreach (var (productId, item) in cart)
            {
                var p = db.Ticket.Find(productId);
                if (p == null) continue;
                purchase.PurchaseItems.Add(new PurchaseItem()
                {
                    Id = NextPurchaseItem_Id(count),

                    Quantity = item.Quantity,
                    validDate = (DateTime)(item.DateOnly?.ToDateTime(TimeOnly.MinValue)),
                    TicketId = p.Id,
                    PurchaseId = purchase.Id,
                });
                count++;
            }
            
            db.Purchase.Add(purchase);
            int changes = db.SaveChanges();
            if (paymentType == "Bank")
            {
                if (changes > 0)
                {
                    var payment = new Payment
                    {
                        Id = NextPayment_Id(),
                        PaymentDateTime = DateTime.Now,
                        Status = "S",
                        Amount = total,
                        Type = "B",
                        Reference = vm.CardNumber,
                        PurchaseId = purchase.Id

                    };
                    db.Payment.Add(payment);
                    int result = db.SaveChanges();
                    if (result > 0)
                    {
                        //set purchase Status
                        var purchaseDB = db.Purchase.FirstOrDefault(p => p.Id == purchase.Id);
                        if (purchaseDB != null)
                        {
                            // Update the Status
                            purchaseDB.Status = "S";

                            // Save changes to the database
                            db.SaveChanges();
                        }
                        var cartTicketIds = cart.Values.Select(item => item.TicketId).ToList();
                        var cartDB = db.Cart
                         .Where(p => p.UserId == userID && cartTicketIds.Contains(p.TicketId))
                         .ToList();
                        db.Cart.RemoveRange(cartDB);
                        db.SaveChanges();
                    }
                    hp.SetCart(cart);
                    return changes.ToString();
                }
                else
                {
                    // No changes were made
                    Console.WriteLine("No changes were made to the database.");
                    return changes.ToString();
                }
            }
            if (paymentType == "Paypal")
            {
                if (changes > 0)
                {
                    hp.SetCart(cart);
                    var cartTicketIds = cart.Values.Select(item => item.TicketId).ToList();
                    var cartDB = db.Cart
                     .Where(p => p.UserId == userID && cartTicketIds.Contains(p.TicketId))
                     .ToList();
                    db.Cart.RemoveRange(cartDB);
                    db.SaveChanges();
                    return purchase.Id;
                }
                else
                {
                    // No changes were made
                    Console.WriteLine("No changes were made to the database.");
                    return changes.ToString();
                }
            }
            return "0";

        }
        //============================================
        //END
        //============================================

        //============================================
        //Payment INSERT DB
        //============================================

        //------------
        //PayPal
        //------------
        [HttpPost]
        [Authorize(Roles = "Member")]
        public IActionResult PurchasePaypal([FromBody] PurchasePaypal purchaseData)
        {
            getUserID();
            var userID = hp.GetUserID();
            var changes = CheckOut(userID, null, "Paypal", false);

           
            if (!string.IsNullOrEmpty(changes))
            {
                Payment payment = null;
                try
                {
                    // Process the purchase data
                    payment = new Payment
                    {
                        Id = NextPayment_Id(),
                        PaymentDateTime = DateTime.Now,
                        Status = "F",
                        Amount = decimal.Parse(purchaseData.Amount),
                        Type = "P",
                        Reference = purchaseData.TransactionId,
                        PurchaseId = changes

                    };

                    // Add additional processing, such as storing purchase items
                    db.Payment.Add(payment);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Purchase recorded successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = payment, error = ex.Message });
                }
            }
            return Json(new { success = false, message = "Failed to record purchase." });
        }


        //============================================
        //END
        //============================================

        //============================================
        //Payment ID 
        //============================================

        private string NextPayment_Id()
        {
            // TODO
            string max = db.Payment.Max(t => t.Id) ?? "PY0000";
            int n = int.Parse(max[2..]);

            return (n + 1).ToString("'PY'0000");
        }

        //============================================
        //END
        //============================================


        // ===========================================
        // show result PurchaseHIS
        // ===========================================
        public List<Purchase> getAllPurcahse(bool? unpaid, string? statusTicket, bool? refundPayment)
        {
            getUserID();
            var userID = hp.GetUserID();
            var allPurchases = db.Purchase
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Ticket)
                        .ThenInclude(at => at.Attraction)
                .Include(us => us.User)
                .Include(po=>po.Promotion)
                .Where(p => p.UserId == userID && p.User is Member)
                .OrderByDescending(p => p.PaymentDateTime)
                .ToList();

            if (unpaid == true)
            {
                allPurchases = allPurchases
                    .Where(p => p.Status == "F")
                 .ToList();
                return allPurchases;
            }
            if (refundPayment == true)
            {
                allPurchases = allPurchases
                    .Where(p => p.Status == "R" || p.Status == "M")
                 .ToList();
                return allPurchases;
            }


            if (!string.IsNullOrEmpty(statusTicket))
            {
                allPurchases = allPurchases
                  .Where(p => p.Status == "S") // Compare p.Id with Payment.PurchaseId
                  .ToList();
                if (statusTicket == "Active")
                {
                    allPurchases = allPurchases
                    .Where(p => p.PurchaseItems.Any(pi => pi.validDate.Date >= DateTime.Now.Date))
                    .ToList();

                }
                else
                {
                    allPurchases = allPurchases
                    .Where(p => p.PurchaseItems.Any(pi => pi.validDate.Date < DateTime.Now.Date) &&
                    !p.PurchaseItems.Any(pi => pi.validDate.Date >= DateTime.Now.Date))

                    .ToList();

                }
                return allPurchases;
            }
            allPurchases = allPurchases
           .Where(p => p.Status == "S") // Compare p.Id with Payment.PurchaseId
           .ToList();
            return allPurchases;
        }
        //============================================
        //END
        //============================================

        //============================================
        //PurchaseHIS GET
        //============================================
        [Authorize(Roles = "Member")]
        public IActionResult ClientPaymentHIS(string? Unpaid,
            string? statusTicket, string? message, string? refund, string? refundPayment)
        {
            // Retrieve all PurchaseItem records with related data
            if (!string.IsNullOrEmpty(message))
            {
                TempData["Message"] = message;
            }

            var allPurchases = getAllPurcahse(false, null, null);


            IEnumerable<Purchase> purchaseItems;
            //Purcahse fillter
            if (Unpaid != null)
            {
                purchaseItems = getAllPurcahse(true, null, null); // If no validdate is provided, show all purchase items(Unpaid)
            }
            else
            {

                purchaseItems = allPurchases;  // If no validdate is provided, show all purchase items(paid)
            }

            if (!string.IsNullOrEmpty(statusTicket))
            {
                allPurchases = getAllPurcahse(false, statusTicket, null);
                purchaseItems = allPurchases;

            }
            if (!string.IsNullOrEmpty(refundPayment))
            {
                allPurchases = getAllPurcahse(null, null, true);
                purchaseItems = allPurchases;
            }

            //stating VM
            var viewModel = new PurchaseViewModel();
            var member = purchaseItems.Select(p => p.User as Member).FirstOrDefault();
            var payemntstatus = db.Payment.Include(p => p.Purchase).Where(p => purchaseItems.Select(pi => pi.Id).Contains(p.PurchaseId))
               .ToList();

            if (Request.IsAjax())
            {
                viewModel = new PurchaseViewModel
                {
                    Purchases = purchaseItems,
                    PhotoURL = member?.PhotoURL,
                    Payment = payemntstatus,
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

        [Authorize(Roles = "Member")]
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
                    attractionID= g.Select(pi => pi.Ticket.Attraction.Id).FirstOrDefault(),
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

        [Authorize(Roles = "Member")]
        public ActionResult ClientPurchaseTicket(string? purchaseId, DateTime validDate,string? attraction)
        {
            if (string.IsNullOrEmpty(purchaseId))
            {
                return Json(new { error = "Purchase ID cannot be null or empty." });
            }

            // Retrieve all the PurchaseItems related to the given PurchaseID
            var purchaseItems = db.PurchaseItem
                .Include(pi => pi.Ticket)
                .ThenInclude(at=>at.Attraction)
                .Where(pi => pi.PurchaseId == purchaseId && pi.validDate.Date == validDate.Date
                 && pi.Ticket.Attraction.Id == attraction)
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
        //============================================
        //END
        //============================================

        //============================================
        //PurchaseHIS Update DB
        //============================================

        [Authorize(Roles = "Member")]
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

        //============================================
        //END
        //============================================

        //============================================
        // Refund Request
        //============================================
        [HttpPost]
        public IActionResult RefundPurchaseRequest(string? purchaseID)
        {
            if (purchaseID == null)
            {
                TempData["SuccessMessage"] = "Purchase Not found!" + purchaseID;
                return Json(new { success = false, message = TempData["SuccessMessage"] });
            }

            var purchaseItems = db.Purchase
                .Where(pi => pi.Id == purchaseID)
                .FirstOrDefault();
            if (purchaseItems == null)
            {
                TempData["SuccessMessage"] = "Purchase Not found!";
                return Json(new { success = false, message = TempData["SuccessMessage"] });
            }

            var payment = db.Payment
                .Where(pi => pi.PurchaseId == purchaseID)
                .FirstOrDefault();

            if (payment != null)
            {
                purchaseItems.Status = "M"; // refund
                db.SaveChanges();
                TempData["SuccessMessage"] = "Refund Request Sended successfully!";
                return Json(new { success = true, message = TempData["SuccessMessage"] });
            }

            TempData["SuccessMessage"] = "Try again later!";
            return Json(new { success = false, message = TempData["SuccessMessage"] });
        }

        //===================================
        //Check Discount code
        //===================================
        [HttpGet]
        public IActionResult CheckDiscountCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { isValid = false, message = "Discount code cannot be empty." });
            }

            var discount = db.Promotion
                             .Where(d => d.Code == code && d.PromoStatus == "Active")
                             .Select(d => new { priceDeduction= d.PriceDeduction, promotionID=d.Id})
                             .FirstOrDefault();

            if (discount != null)
            {
                return Json(new
                {
                    isValid = true,
                    message = "Discount Inserted.",
                    priceDeduction = discount.priceDeduction,
                    promotionID = discount.promotionID
                });
            }
            else
            {
                return Json(new { isValid = false, message = "Discount code is invalid or inactive." });
            }
        }

        [HttpPost]
        public IActionResult DeletePayment(string? purchaseID)
        {
            if (purchaseID == null)
            {
                return Json(new { success = false, message = "Purchase Not found!" });
            }

            var purchaseItems = db.Purchase
                .FirstOrDefault(pi => pi.Id == purchaseID);

            if (purchaseItems == null)
            {
                return Json(new { success = false, message = "Purchase Not found!" });
            }

            // Retrieve the associated Payment
            var payment = db.Payment
                .FirstOrDefault(pi => pi.PurchaseId == purchaseID);

            var purchaseItem=db.PurchaseItem
                .Where(p=>p.PurchaseId==purchaseID)
                .ToList();
            if (payment != null)
            {
                // If the payment exists, we proceed with the deletion
                // First delete the related PurchaseItems
                db.PurchaseItem.RemoveRange(purchaseItem);

                // Then delete the Payment
                db.Payment.Remove(payment);

                // Finally, delete the Purchase itself
                db.Purchase.Remove(purchaseItems);

                // Save the changes to the database
                db.SaveChanges();

                return Json(new { success = true, message = "Purchase and related payment successfully deleted!" });
            }

            return Json(new { success = false, message = "Payment not found or unable to delete!" });
        }

    }
}
