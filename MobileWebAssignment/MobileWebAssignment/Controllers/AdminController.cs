using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileWebAssignment.Models;
using X.PagedList.Extensions;

namespace MobileWebAssignment.Controllers;

public class AdminController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public AdminController(DB db, IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
    }
    
    public IActionResult AdminDiscount()
    {
        return View();
    }

//==================================== Attraction Type start =========================================================
    public IActionResult AdminAttraction(int ATpage = 1, int Apage = 1)
    {
        //page list for Attraction Types
        if (ATpage < 1)
        {
            return RedirectToAction(null, new { ATpage = 1 });
        }

        ViewBag.AttractionTypes = db.AttractionType.ToPagedList(ATpage, 5);

        if (ATpage > ViewBag.AttractionTypes.PageCount && ViewBag.AttractionTypes.PageCount > 0)
        {
            return RedirectToAction(null, new { ATpage = ViewBag.AttractionTypes.PageCount });
        }


        //page list for Attractions
        if (Apage < 1)
        {
            return RedirectToAction(null, new { Apage = 1 });
        }

        var attractions = db.Attraction.Include(a => a.AttractionType).ToPagedList(Apage, 5);

        if (Apage > attractions.PageCount && attractions.PageCount > 0)
        {
            return RedirectToAction(null, new { Apage = attractions.PageCount });
        }


        return View(attractions);
    }

    // Manually generate next id for attraction type
    private string NextAttractionTypeId()
    {
        string max = db.AttractionType.Max(s => s.Id) ?? "AT000";
        int n = int.Parse(max[2..]);
        return (n + 1).ToString("'AT'000");
    }

    // GET: AttractionType/Insert
    public IActionResult AdminAttractionTypeCreate()
    {
        var vm = new AttractionTypeInsertVM
        {
            Id = NextAttractionTypeId(),
        };

        return View(vm);
    }

    // POST: AttractionType/Insert
    [HttpPost]
    public IActionResult AdminAttractionTypeCreate(AttractionTypeInsertVM vm)
    {
        if (ModelState.IsValid("Name") && db.AttractionType.Any(at => at.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "This attraction type already added before, please change other name");
        }
        
        if (ModelState.IsValid)
        {
            db.AttractionType.Add(new()
            {
                Id = vm.Id,
                Name = vm.Name.Trim(),
            });
            db.SaveChanges();

            TempData["Info"] = "Attraction Type inserted.";
            return RedirectToAction("AdminAttraction");
        }

        return View();
    }

    // GET: AttractionType/Update
    public IActionResult AdminAttractionTypeUpdate(string? Id)
    {
        var at = db.AttractionType.Find(Id);

        if (at == null)
        {
            return RedirectToAction("AdminAttraction");
        }

        var vm = new AttractionTypeInsertVM
        {
            Id = at.Id,
            Name= at.Name,
        };

        return View(vm);
    }

    // POST: AttractionType/Update
    [HttpPost]
    public IActionResult AdminAttractionTypeUpdate(AttractionTypeInsertVM vm)
    {
        var at = db.AttractionType.Find(vm.Id);

        if (at == null)
        {
            return RedirectToAction("AdminAttraction");
        }

       

        if (ModelState.IsValid)
        {

            at.Id = vm.Id;
            at.Name = vm.Name.Trim();
            db.SaveChanges();

            TempData["Info"] = "Record Updated.";
            return RedirectToAction("AdminAttraction");

        }

        return View(vm);
    }


    // GET: AttractionType/Delete
    public IActionResult AdminAttractionTypeDelete(string? Id)
    {
        var at = db.AttractionType.Find(Id);

        if (at == null)
        {
            return RedirectToAction("AdminAttraction");
        }

        var vm = new AttractionTypeInsertVM
        {
            Id = at.Id,
            Name = at.Name,
        };

        return View(vm);
    }

    // POST: AttractionType/Delete
    [HttpPost]
    public IActionResult AdminAttractionTypeDelete(AttractionTypeInsertVM vm)
    {
        var at = db.AttractionType.Find(vm.Id);

        if (at != null) 
        {
            db.AttractionType.Remove(at);
            db.SaveChanges();
            TempData["Info"] = "Record Deleted.";
        }

        return RedirectToAction("AdminAttraction");
    }

    //============================================ Attraction Type end ====================================================

    //============================================ Attraction start =======================================================
    private string NextAttractionId()
    {
        string max = db.Attraction.Max(s => s.Id) ?? "A000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'A'000");
    }

    // GET: AttractionType/Insert
    public IActionResult AdminAttractionCreate()
    {
        ViewBag.AttractionTypes = db.AttractionType.ToList();

        var vm = new AttractionInsertVM
        {
            Id = NextAttractionId(),
            OperatingHours = "none",
        };

        return View(vm);
    }

    // POST: AttractionType/Insert
    [HttpPost]
    public IActionResult AdminAttractionCreate(AttractionInsertVM vm)
    {

        ViewBag.AttractionTypes = db.AttractionType.ToList();
        

        //check name
        if (ModelState.IsValid("Name") && db.Attraction.Any(a => a.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "This attraction already added before, please change other name");
        }

        //null check for attraction type id
        if (ModelState.IsValid("AttractionTypeId") && vm.AttractionTypeId.Equals("none"))
        {
            ModelState.AddModelError("AttractionTypeId", "Please select an attraction type");
        }

        //check photo
        if (ModelState.IsValid("Photo"))
        {
            var e = hp.ValidatePhoto(vm.Photo);
            if (e != "") ModelState.AddModelError("Photo", e);
        }

        //perform validation
        int errorCount = 0;
        int i = 0;
        foreach (var operateTime in vm.operatingHours)
        {
            if(operateTime.Status == "open")
            {
                if (operateTime.StartTime == null || operateTime.EndTime == null)
                {
                    ModelState.AddModelError("operatingHours[" + i + "]", $"Operating hours for {operateTime.Day} are incomplete.");
                }else if(operateTime.StartTime > operateTime.EndTime)
                {
                    ModelState.AddModelError("operatingHours[" + i + "]", $"End Time cannot less than Start Time for {operateTime.Day} .");
                }
            }
            i++;
        }

        string operateHours = "";
        if(errorCount == 0)
        { 
            //combine and get the complete operating hours
            for (i = 0; i < 7 ; i++)
            {
                operateHours += vm.operatingHours[i].Day + " " + vm.operatingHours[i].Status + " " + vm.operatingHours[i].StartTime + " " + vm.operatingHours[i].EndTime + " | ";
            }
        }

        vm.OperatingHours = operateHours;
        
        //all attribute in view model is correct, then insert new record
        if (ModelState.IsValid)
        { 

            db.Attraction.Add(new()
            {   
                Id = vm.Id,
                Name = vm.Name.Trim(),
                Description = vm.Description.Trim(),
                Location = vm.Location.Trim(),
                OperatingHours = vm.OperatingHours,
                ImagePath = hp.SavePhoto(vm.Photo, "attractionImages"),
                AttractionTypeId = vm.AttractionTypeId,
            });
            db.SaveChanges();

            TempData["Info"] = "Attraction inserted.";
            return RedirectToAction("AdminAttraction");
        }


        return View(vm);
    }


    // GET: Attraction/Update
    public IActionResult AdminAttractionUpdate(string? Id)
    {
        var a = db.Attraction.Find(Id);
        ViewBag.AttractionTypes = db.AttractionType.ToList();

        if (a == null)
        {
            return RedirectToAction("AdminAttraction");
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
            operatingHours = hp.ParseOperatingHours(a.OperatingHours),
        };

        return View(vm);
    }

    // POST: Attraction/Update
    [HttpPost]
    public IActionResult AdminAttractionUpdate(AttractionUpdateVM vm)
    {
        var a = db.Attraction.Find(vm.Id);
        ViewBag.AttractionTypes = db.AttractionType.ToList();

        //check vm model
        if (a == null)
        {
            return RedirectToAction("AdminAttraction");
        }

        //check photo if have
        if (vm.Photo != null)
        {
            var e = hp.ValidatePhoto(vm.Photo);
            if (e != "") ModelState.AddModelError("Photo", e);
        }

        //perform validation
        int errorCount = 0;
        int i = 0;
        foreach (var operateTime in vm.operatingHours)
        {
            if (operateTime.Status == "open")
            {
                if (operateTime.StartTime == null || operateTime.EndTime == null)
                {
                    ModelState.AddModelError("operatingHours[" + i + "]", $"Operating hours for {operateTime.Day} are incomplete.");
                }
                else if (operateTime.StartTime > operateTime.EndTime)
                {
                    ModelState.AddModelError("operatingHours[" + i + "]", $"End Time cannot less than Start Time for {operateTime.Day} .");
                }
            }
            i++;
        }

        string operateHours = "";
        if (errorCount == 0)
        {
            //combine and get the complete operating hours
            for (i = 0; i < 7; i++)
            {
                operateHours += vm.operatingHours[i].Day + " " + vm.operatingHours[i].Status + " " + vm.operatingHours[i].StartTime + " " + vm.operatingHours[i].EndTime + " | ";
            }
        }

        vm.OperatingHours = operateHours;


        if (ModelState.IsValid)
        {

            a.Id = vm.Id;
            a.Name = vm.Name.Trim();
            a.Description = vm.Description.Trim();
            a.Location = vm.Location.Trim();
            a.OperatingHours = vm.OperatingHours;
            a.AttractionTypeId = vm.AttractionTypeId;
            if (vm.Photo != null)
            {
                hp.DeletePhoto(a.ImagePath, "attractionImages");
                a.ImagePath = hp.SavePhoto(vm.Photo, "attractionImages");
            }
            db.SaveChanges();

            TempData["Info"] = "Record Updated.";
            return RedirectToAction("AdminAttraction");

        }

        return View(vm);
    }


    // GET: Attraction/Delete
    public IActionResult AdminAttractionDelete(string? Id)
    {
        var a = db.Attraction.Find(Id);

        if (a == null)
        {
            return RedirectToAction("AdminAttraction");
        }

        var vm = new AttractionInsertVM
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            Location = a.Location,
            OperatingHours = a.OperatingHours,
            ImagePath = a.ImagePath,
            AttractionTypeId = a.AttractionTypeId,
        };

        return View(vm);
    }

    // POST: Attraction/Delete
    [HttpPost]
    public IActionResult AdminAttractionDelete(AttractionInsertVM vm)
    {
        var a = db.Attraction.Find(vm.Id);

        if (a != null)
        {
            hp.DeletePhoto(a.ImagePath, "attractionImages");
            db.Attraction.Remove(a);
            db.SaveChanges();
            TempData["Info"] = "Record Deleted.";
        }

        return RedirectToAction("AdminAttraction");
    }


    //============================================ Attraction end =========================================================
    //============================================ Feedback start =========================================================
    public IActionResult AdminFeedback()
    {
        var feedbacks = db.Feedback.Include(a => a.Attraction).Include(u => u.User).ToList();

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



    //============================================ Feedback end =========================================================



    //============================================ Member Maintenance =========================================================

    // Generate ID for create account
    private string NextRegisterId()
    {
        string max = db.User.Max(s => s.Id) ?? "U000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'U'000");
    }

    // GET Admin/CreateAccount
    public IActionResult CreateAccount()
    {
        return View();
    }

    //POST Admin/CreateAccount
    [HttpPost]
    public IActionResult CreateAccount(RegisterVM vm)
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
            TempData["Info"] = "Create successfully. Please login.";
            return RedirectToAction("MemberList");
        }

        return View(vm);
    }

    //GET Admin/MemberList
    public IActionResult MemberList(int page = 1)
    {
        // Ensure the page number is at least 1
        if (page < 1)
        {
            return RedirectToAction(null, new { page = 1 });
        }

        // Get paged list of users from the database
        ViewBag.Users = db.User.ToPagedList(page, 5);

        // Ensure the page number does not exceed the total page count
        if (page > ViewBag.Users.PageCount && ViewBag.Users.PageCount > 0)
        {
            return RedirectToAction(null, new { page = ViewBag.Users.PageCount });
        }

        // Return the view with the paged list
        return View(ViewBag.Users);
    }


    //POST : UserStatus
    [HttpPost]
    public IActionResult UserStatus(string id)
    {
        // Find the user in the database
        var user = db.User.FirstOrDefault(u => u.Id == id);

        if (user != null)
        {
            // Toggle the Freeze status
            user.Freeze = !user.Freeze;

            // Save changes to the database
            db.SaveChanges();

            string action = user.Freeze ? "blocked" : "unblocked";
            TempData["SuccessMessage"] = $"User with ID {id} has been {action}.";
            return RedirectToAction("MemberList");
        }

        TempData["ErrorMessage"] = "User not found.";
        return RedirectToAction("MemberList");
    }

    //GET Admin/AdminUpdateMember
    [HttpGet("Admin/AdminUpdateMember/{Id?}")]
    public IActionResult AdminUpdateMember(string? Id)
    {
        var user = db.Members.Find(Id);

        if (user == null) return RedirectToAction("MemberList", "Admin");

        var photo = $"/User/{user.PhotoURL}";

        var birthDate = Helper.ConvertIcToBirthDate(user.IC);

        var vm = new UpdateProfileVm
        {
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            IC = user.IC,
            BirthDate = birthDate,
            PhotoURL = photo,
        };

        return View(vm);
    }

    //POST Admin/AdminUpdateMember 
    [HttpPost("Admin/AdminUpdateMember/{Id?}")]
    public IActionResult AdminUpdateMember(UpdateProfileVm vm,string? Id)
    {
        var user = db.Members.Find(Id);

        if (user == null) return RedirectToAction("MemberList", "Admin");

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
            return RedirectToAction("MemberList", "Admin");
        }

        return View(vm);

    }


    //============================================ Member Maintenance End =========================================================

}
