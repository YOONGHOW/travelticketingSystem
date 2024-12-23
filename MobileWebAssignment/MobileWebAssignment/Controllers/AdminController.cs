using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MobileWebAssignment.Models;
using System.Net.Sockets;
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
    //============================================ Ticket start =========================================================

    public IActionResult AdminTicket()
    {
        ViewBag.AttractionTypes = db.AttractionType;
        var attractions = db.Attraction.Include(a => a.AttractionType);

        return View(attractions);
    }
    //Auto generate id
    private string NextTicketId()
    {
        string max = db.Ticket.Max(s => s.Id) ?? "TK0000";
        int n = int.Parse(max[2..]); 
        return (n + 1).ToString("'TK'0000"); 
    }
    // Insert ticket
    [HttpGet]
    public IActionResult AdminTicketInsert(string id)
    {
        var attraction = db.Attraction.FirstOrDefault(a => a.Id == id);
        if (attraction == null)
        {
            return NotFound();
        }

        var vm = new TicketVM
        {
            AttractionId = id 
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult AdminTicketInsert(TicketVM vm)
    {
        if (ModelState.IsValid)
        {
            db.Ticket.Add(new()
            {
                Id = NextTicketId(),
                ticketName = vm.ticketName,
                stockQty = vm.stockQty,
                ticketPrice = vm.ticketPrice,
                ticketStatus = vm.ticketStatus,
                ticketDetails = vm.ticketDetails,
                ticketType = vm.ticketType,
                AttractionId = vm.AttractionId,
            });
            db.SaveChanges();

            TempData["Info"] = "Record Insert";
            return RedirectToAction("AdminTicketDetails", new { id = vm.AttractionId });
        }
        return View(vm);
    }

    //Update ticket

    //Get ticket
    public IActionResult AdminTicketUpdate(string? Id)
    {
        var t = db.Ticket.Find(Id);

        if (t == null)
        {
            return RedirectToAction("Index");
        }
        var vm = new TicketVM
        {
            Id = t.Id,
            ticketName = t.ticketName,
            stockQty = t.stockQty,
            ticketPrice = t.ticketPrice,
            ticketStatus = t.ticketStatus,
            ticketDetails = t.ticketDetails,
            ticketType = t.ticketType,
            AttractionId = t.AttractionId,
        };

        ViewBag.ProgramList = new SelectList(db.Ticket, "Id", "Name");
        return View(vm);
    }

    [HttpPost]
    public IActionResult AdminTicketUpdate(TicketVM vm)
    {
        var t = db.Ticket.Find(vm.Id);

        if(t == null)
        {
            return RedirectToAction("AdminTicketDetails");
        }

        if (ModelState.IsValid) {
            t.ticketName = vm.ticketName.Trim();
            t.stockQty = vm.stockQty;
            t.ticketPrice = vm.ticketPrice;
            t.ticketStatus = vm.ticketStatus;
            t.ticketDetails = vm.ticketDetails;
            t.ticketType = vm.ticketType;
            t.AttractionId = vm.AttractionId;
            db.SaveChanges();

            TempData["Info"] = "Record updated.";
            return RedirectToAction("AdminTicketDetails", new { id = vm.AttractionId });

        };
        ViewBag.ProgramList = new SelectList(db.Ticket, "Id", "Name");
        return View(vm);
    }

    //display attraction and ticket details
    public IActionResult AdminTicketDetails(string id)
    {
        var attraction = db.Attraction.Include(a => a.AttractionType)
                                       .FirstOrDefault(a => a.Id == id);

        if (attraction == null)
        {
            return NotFound();
        }

        var tickets = db.Ticket.Where(t => t.AttractionId == id).ToList();
        var vm = new AdminTicketDetails
        {
            Attraction = attraction,
            Tickets = tickets
        };
        return View(vm);
    }

    //Delete ticket
    public ActionResult AdminTicketDelete(string? Id)
    {
        var t = db.Ticket.Find(Id);

        if (t == null)
        {
            return RedirectToAction("AdminTicketDetails");
        }
        var vm = new TicketVM
        {
            Id = t.Id,
            ticketName = t.ticketName,
            stockQty = t.stockQty,
            ticketPrice = t.ticketPrice,
            ticketStatus = t.ticketStatus,
            ticketDetails = t.ticketDetails,
            ticketType = t.ticketType,
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult AdminTicketDelete(TicketVM vm)
    {
        var t = db.Ticket.Find(vm.Id);

        if (t != null)
        {
            string attractionId = t.AttractionId;
            db.Ticket.Remove(t);
            db.SaveChanges();
            TempData["Info"] = "Record Deleted.";

            return RedirectToAction("AdminTicketDetails", new { id = attractionId });
        }
        return RedirectToAction("AdminTicketDetails", new { id = vm.AttractionId });
    }
}
