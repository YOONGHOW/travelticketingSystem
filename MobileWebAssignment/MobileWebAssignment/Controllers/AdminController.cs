
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;

using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font;
using iText.Kernel.Font;
using System.IO;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using iText.Layout.Borders;
using iText.Kernel.Colors;
using MobileWebAssignment.Models;



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



    //==================================== Attraction Type start =========================================================

    [Authorize(Roles = "Admin")]
    public IActionResult AdminAttraction(string? Aname, string? Asort, string? Adir, int ATpage = 1, int Apage = 1)
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


        // Searching for attraction
        ViewBag.aname = Aname = Aname?.Trim() ?? "";

        var atts = db.Attraction.Include(a => a.AttractionType).Where(a => a.Name.Contains(Aname));

        // Sorting for attraction
        ViewBag.ASort = Asort;
        ViewBag.ADir = Adir;

        Func<Attraction, object> attraction = Asort switch
        {
            "Id" => a => a.Id,
            "Name" => a => a.Name,
            "Location" => a => a.Location,
            "AttractionTypeId" => a => a.AttractionTypeId,
            _ => a => a.Id,
        };

        var at = Adir == "des" ?
                atts.OrderByDescending(attraction) :
                atts.OrderBy(attraction);

        //page list for Attractions
        if (Apage < 1)
        {
            return RedirectToAction(null, new { Apage = 1 });
        }

        var attractions = at.ToPagedList(Apage, 5);

        if (Apage > attractions.PageCount && attractions.PageCount > 0)
        {
            return RedirectToAction(null, new { Aname, Asort, Adir, Apage = attractions.PageCount });
        }

        if (Request.IsAjax())
        {
            return PartialView("_AdminAttraction", attractions);
        }

        return View(attractions);
    }

    //ajax function for attraction type
    public IActionResult AjaxAttractionType(string? ATname, string? ATsort, string? ATdir, int ATpage = 1)
    {
        //Searching for attraction type
        ViewBag.atname = ATname = ATname?.Trim() ?? "";
        var attractionTypes = db.AttractionType.Where(at => at.Name.Contains(ATname));

        // Sorting for attraction
        ViewBag.ATSort = ATsort;
        ViewBag.ATDir = ATdir;

        Func<AttractionType, object> attraction = ATsort switch
        {
            "Id" => at => at.Id,
            "Name" => at => at.Name,
            _ => at => at.Id,
        };

        var at = ATdir == "des" ?
                attractionTypes.OrderByDescending(attraction) :
                attractionTypes.OrderBy(attraction);

        //page list for Attraction Types
        if (ATpage < 1)
        {
            return RedirectToAction(null, new { ATpage = 1 });
        }

        var atType = at.ToPagedList(ATpage, 5);
        ViewBag.AttractionTypes = atType;

        if (ATpage > ViewBag.AttractionTypes.PageCount && ViewBag.AttractionTypes.PageCount > 0)
        {
            return RedirectToAction(null, new { ATname, ATsort, ATdir, ATpage = ViewBag.AttractionTypes.PageCount });
        }

        return PartialView("_AdminAttractionType");
    }

    // Manually generate next id for attraction type
    private string NextAttractionTypeId()
    {
        string max = db.AttractionType.Max(s => s.Id) ?? "AT000";
        int n = int.Parse(max[2..]);
        return (n + 1).ToString("'AT'000");
    }

    // GET: AttractionType/Insert
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
            Name = at.Name,
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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

            if (vm.Photo.images.Count == 0)
            {
                ModelState.AddModelError("Photo", "Please upload the attraction image(s)");
            }
            else
            {
                var e = hp.ValidateMultiplePhoto(vm.Photo.images);
                if (e != "") ModelState.AddModelError("Photo.images", e);
            }
        }

        //checked Location
        if (ModelState.IsValid("Location"))
        {
            string validation = hp.ValidateMalaysianAddress(vm.Location);

            if (!validation.Equals("valid"))
            {
                ModelState.AddModelError("Location", validation);
            }
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
                ImagePath = hp.SaveMultiplePhoto(vm.Photo.images, "attractionImages"),
                AttractionTypeId = vm.AttractionTypeId,
            });
            db.SaveChanges();

            TempData["Info"] = "Attraction inserted.";
            return RedirectToAction("AdminAttraction");
        }


        return View(vm);
    }


    // GET: Attraction/Update
    [Authorize(Roles = "Admin")]
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
            Photo = new UpdateImageSet(),
        };

        vm.Photo.imagePaths = hp.SplitImagePath(a.ImagePath);

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


        //null check for attraction type id
        if (ModelState.IsValid("AttractionTypeId") && vm.AttractionTypeId.Equals("none"))
        {
            ModelState.AddModelError("AttractionTypeId", "Please select an attraction type");
        }

        //checked Location
        if (ModelState.IsValid("Location"))
        {
            string validation = hp.ValidateMalaysianAddress(vm.Location);

            if (!validation.Equals("valid"))
            {
                ModelState.AddModelError("Location", validation);
            }
        }

        //check photo if have
        if (vm.Photo != null)
        {
            var e = hp.ValidateMultiplePhoto(vm.Photo.images);
            if (e != "") ModelState.AddModelError("Photo.images", e);
        }
        else
        {
            vm.Photo = new UpdateImageSet();
        }

        vm.Photo.imagePaths = hp.SplitImagePath(a.ImagePath);

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
            if (vm.Photo != null && vm.Photo.images != null)
            {
                hp.DeleteMultiplePhoto(a.ImagePath, "attractionImages");
                a.ImagePath = hp.SaveMultiplePhoto(vm.Photo.images, "attractionImages");
            }
            db.SaveChanges();

            TempData["Info"] = "Record Updated.";
            return RedirectToAction("AdminAttraction");

        }




        return View(vm);
    }


    // GET: Attraction/Delete
    [Authorize(Roles = "Admin")]
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
            Photo = new ImageSet(),
        };

        vm.Photo.imagePaths = hp.SplitImagePath(a.ImagePath);

        return View(vm);
    }

    // POST: Attraction/Delete
    [HttpPost]
    public IActionResult AdminAttractionDelete(AttractionInsertVM vm)
    {
        var a = db.Attraction.Find(vm.Id);

        if (a != null)
        {
            hp.DeleteMultiplePhoto(a.ImagePath, "attractionImages");
            db.Attraction.Remove(a);
            db.SaveChanges();
            TempData["Info"] = "Record Deleted.";
        }

        return RedirectToAction("AdminAttraction");
    }


    //============================================ Attraction end =========================================================
    //============================================ Ticket start =========================================================

    [Authorize(Roles = "Admin")]
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
        int n = int.Parse(max.Substring(2)); // Skip the "TK" prefix
        return $"TK{(n + 1):D4}"; // This ensures the next ID has a leading zero if necessary
    }
    // Insert ticket
    [Authorize(Roles = "Admin")]
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

            TempData["Info"] = "A ticket for " + vm.AttractionId + "  is inserted.";
            return RedirectToAction("AdminTicketDetails", new { id = vm.AttractionId });
        }
        return View(vm);
    }

    //Update ticket

    //Get ticket
    [Authorize(Roles = "Admin")]
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

        if (t == null)
        {
            return RedirectToAction("AdminTicketDetails");
        }

        if (ModelState.IsValid)
        {
            t.ticketName = vm.ticketName.Trim();
            t.stockQty = vm.stockQty;
            t.ticketPrice = vm.ticketPrice;
            t.ticketStatus = vm.ticketStatus;
            t.ticketDetails = vm.ticketDetails;
            t.ticketType = vm.ticketType;
            t.AttractionId = vm.AttractionId;
            db.SaveChanges();

            TempData["Info"] = "Ticket " + vm.Id + " has been updated.";
            return RedirectToAction("AdminTicketDetails", new { id = vm.AttractionId });

        };
        ViewBag.ProgramList = new SelectList(db.Ticket, "Id", "Name");
        return View(vm);
    }

    //display attraction and ticket details
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
            AttractionId = t.AttractionId,

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
            TempData["Info"] = "A ticket for " + t.AttractionId + " has been deleted.";

            return RedirectToAction("AdminTicketDetails", new { id = attractionId });
        }
        return RedirectToAction("AdminTicketDetails", new { id = vm.AttractionId });
    }


    [HttpPost]
    public IActionResult Import(IFormFile file)
    {
        if (file != null
            && file.FileName.EndsWith(".txt")
            && file.ContentType == "text/plain")
        {
            int n = ImportTicket(file);
            TempData["Info"] = $"{n} tickets imported.";
        }

        return RedirectToAction("AdminTicket");
    }

    private int ImportTicket(IFormFile file)
    {
        // Read from uploaded file --> import events
        // Return number new events inserted
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine() ?? "";
            if (line.Trim() == "") continue;
            var data = line.Split("\t", StringSplitOptions.TrimEntries);

            if (!int.TryParse(data[1], out int stockQty))
            {
                throw new FormatException($"Invalid stock quantity: {data[1]}");
            }

            if (!decimal.TryParse(data[2], out decimal ticketPrice))
            {
                throw new FormatException($"Invalid ticket price: {data[2]}");
            }



            db.Ticket.Add(new()
            {

                Id = NextTicketId(),
                ticketName = data[0],
                stockQty = stockQty,
                ticketPrice = ticketPrice,
                ticketStatus = data[3],
                ticketDetails = data[4],
                ticketType = data[5],
                AttractionId = data[6],
            });
        }

        return db.SaveChanges();
    }
    //============================================ Feedback start =========================================================

    [Authorize(Roles = "Admin")]
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
                insertReplyFeedback = new FeedbackReplyVM(),
                feedbackReplyList = db.FeedbackReply.Where(fr => fr.FeedbackId == f.Id).ToList(),
            });
        }

        return View(vm);
    }

    public IActionResult FilterFeedback(string? attractionId, string dateSort, string star)
    {

        var feedbacks = db.Feedback.Include(a => a.Attraction)
                                   .Include(u => u.User)
                                   .Where(f => attractionId == "all" || f.AttractionId == attractionId)
                                   .Where(f => f.Rating == int.Parse(star))
                                   .ToList();

        //sorting
        var sortedFeedbacks = dateSort.ToLower() == "asc"
           ? feedbacks.OrderBy(f => f.SubmitDate)
           : feedbacks.OrderByDescending(f => f.SubmitDate);

        ViewBag.attractions = db.Attraction.ToList();

        var vm = new List<FeedbackInsertVM>();
        foreach (var f in sortedFeedbacks)
        {
            vm.Add(new FeedbackInsertVM
            {
                Id = f.Id,
                Comment = f.Comment,
                Rating = f.Rating,
                SubmitDate = f.SubmitDate,
                AttractionId = f.AttractionId,
                commentDetail = hp.ConvertComment(f.Comment),
                insertReplyFeedback = new FeedbackReplyVM(),
                feedbackReplyList = db.FeedbackReply.Where(fr => fr.FeedbackId == f.Id).ToList(),
            });
        }

        return PartialView("_AdminFeedback", vm);
    }

    private string NextFeedbackReplyId()
    {
        string max = db.FeedbackReply.Max(fr => fr.Id) ?? "FR000";
        int n = int.Parse(max[2..]);
        return (n + 1).ToString("'FR'000");
    }

    [HttpPost]
    public IActionResult AddComment(string reply, string feedbackId, string comment)
    {
        ViewBag.attractions = db.Attraction.ToList();


        db.FeedbackReply.Add(new FeedbackReply
        {
            Id = NextFeedbackReplyId(),
            Comment = comment,
            Type = reply.Trim(),
            FeedbackId = feedbackId,
        });
        db.SaveChanges();

        TempData["Info"] = "Comment send successfully.";


        return RedirectToAction("AdminFeedback");
    }

    [HttpPost]
    public IActionResult DeleteComment(string replyId)
    {

        ViewBag.attractions = db.Attraction.ToList();

        var fr = db.FeedbackReply.Find(replyId);

        if (fr != null)
        {
            db.FeedbackReply.Remove(fr);
            db.SaveChanges();
            TempData["Info"] = "Record Deleted.";
            return RedirectToAction("AdminFeedback");
        }

        return RedirectToAction("AdminFeedback");
    }
    //============================================ Feedback end =========================================================

    //============================================ Promotion start =========================================================

    [Authorize(Roles = "Admin")]
    public IActionResult AdminDiscount(string name = "")
    {
        // Filter promotions by Title or Code based on the search term
        var promotions = db.Promotion
            .Where(p => string.IsNullOrEmpty(name) ||
                        p.Title.Contains(name) ||
                        p.Code.Contains(name))
            .OrderBy(p => p.Id)
            .Select(p => new PromotionInsertVM
            {
                Id = p.Id,
                Title = p.Title,
                Code = p.Code,
                PriceDeduction = p.PriceDeduction,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                PromoStatus = p.PromoStatus
            })
            .ToList();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            // Return only the table HTML if the request is AJAX
            string tableHtml = GenerateTableHtml(promotions);
            return Content(tableHtml, "text/html");
        }

        // Return the full view for normal requests
        return View(promotions);
    }

    private string GenerateTableHtml(List<PromotionInsertVM> promotions)
    {
        string tableHtml = "<table><thead><tr>" +
                           "<th>Discount ID</th><th>Title</th><th>Discount Amount</th>" +
                           "<th>Start Date</th><th>End Date</th><th>Status</th>" +
                           "<th colspan='2' style='text-align:center;'>Action</th></tr></thead><tbody>";

        foreach (var promo in promotions)
        {
            tableHtml += $"<tr>" +
                         $"<td>{promo.Id}</td>" +
                         $"<td>{promo.Title}</td>" +
                         $"<td>{promo.PriceDeduction}</td>" +
                         $"<td>{promo.StartDate:yyyy-MM-dd}</td>" +
                         $"<td>{promo.EndDate:yyyy-MM-dd}</td>" +
                         $"<td>{promo.PromoStatus}</td>" +
                         $"<td style='text-align:center;'>" +
                         $"<a href='/Admin/AdminDiscountUpdate/{promo.Id}'><button class='update'>Update</button></a>" +
                         $"</td>" +
                         $"<td style='text-align:center;'>" +
                         $"<a href='/Admin/AdminDiscountDelete/{promo.Id}'><button class='delete'>Delete</button></a>" +
                         $"</td>" +
                         $"</tr>";
        }

        tableHtml += "</tbody></table>";
        return tableHtml;
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // Pool of characters
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }


    // GET: Admin/AdminDiscountCreate
    [Authorize(Roles = "Admin")]
    public IActionResult AdminDiscountCreate()
    {
        var autoGeneratedId = $"PM{(db.Promotion.Count() + 1).ToString("D4")}"; // Example of auto-generating ID
        var currentDate = DateTime.Now.Date; // Get the current date without the time component

        var randomCode = GenerateRandomCode(6); // Generate a random 6-character code

        var vm = new PromotionInsertVM
        {
            Id = autoGeneratedId,
            StartDate = currentDate,
            EndDate = currentDate,
            Code = randomCode // Set the auto-generated code
        };

        return View(vm);
    }

    // POST: Admin/AdminDiscountCreate
    [HttpPost]
    public IActionResult AdminDiscountCreate(PromotionInsertVM vm)
    {
        // Check for duplicated Promotion ID
        if (ModelState.IsValid("Id") && db.Promotion.Any(p => p.Id == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated Promotion ID.");
        }

        // Check for duplicated Promotion Code
        if (ModelState.IsValid("Code") && db.Promotion.Any(p => p.Code == vm.Code))
        {
            ModelState.AddModelError("Code", "Duplicated Promotion Code.");
        }

        // Ensure Price Deduction is valid
        if (ModelState.IsValid("PriceDeduction") && vm.PriceDeduction <= 0)
        {
            ModelState.AddModelError("PriceDeduction", "Price Deduction must be greater than 0.");
        }

        // Ensure Start Date is before End Date
        if (ModelState.IsValid("StartDate") && ModelState.IsValid("EndDate"))
        {
            if (vm.StartDate >= vm.EndDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
            }
        }

        // Validate PromoStatus (optional validation for better UX)
        if (string.IsNullOrWhiteSpace(vm.PromoStatus) || !(vm.PromoStatus == "Active" || vm.PromoStatus == "Inactive"))
        {
            ModelState.AddModelError("PromoStatus", "Invalid Promotion Status.");
        }

        // If the model is valid, save the Promotion to the database
        if (ModelState.IsValid)
        {
            db.Promotion.Add(new Promotion
            {
                Id = vm.Id.Trim().ToUpper(),
                Title = vm.Title.Trim(),
                Code = vm.Code.Trim().ToUpper(),
                PriceDeduction = vm.PriceDeduction,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                PromoStatus = vm.PromoStatus // Save the selected PromoStatus
            });
            db.SaveChanges();

            TempData["Info"] = "Promotion created successfully.";
            return RedirectToAction("AdminDiscount");
        }

        // If there are validation errors, return the same view with the error messages
        return View(vm);
    }


    // GET: Admin/AdminDiscountDelete/{id}
    [Authorize(Roles = "Admin")]
    public IActionResult AdminDiscountDelete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "Invalid ID.";
            return RedirectToAction("AdminDiscount");
        }

        var promotion = db.Promotion.FirstOrDefault(p => p.Id == id);
        if (promotion == null)
        {
            TempData["Error"] = "Promotion not found.";
            return RedirectToAction("AdminDiscount");
        }

        var vm = new PromotionInsertVM
        {
            Id = promotion.Id,
            Title = promotion.Title,
            Code = promotion.Code,
            PriceDeduction = promotion.PriceDeduction,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            PromoStatus = promotion.PromoStatus
        };

        return View(vm);
    }

    // POST: Admin/AdminDiscountDelete
    [HttpPost]
    public IActionResult AdminDiscountDelete(PromotionInsertVM vm)
    {
        var promotion = db.Promotion.FirstOrDefault(p => p.Id == vm.Id);

        if (promotion == null)
        {
            TempData["Error"] = "Promotion not found.";
            return RedirectToAction("AdminDiscount");
        }

        db.Promotion.Remove(promotion);
        db.SaveChanges();

        TempData["Info"] = "Promotion deleted successfully.";
        return RedirectToAction("AdminDiscount");
    }

    // GET: Admin/DiscountUpdate
    [Authorize(Roles = "Admin")]
    public IActionResult AdminDiscountUpdate(string? id)
    {
        var promotion = db.Promotion.Find(id);

        if (promotion == null)
        {
            return RedirectToAction("AdminDiscount");
        }

        var vm = new PromotionInsertVM
        {
            Id = promotion.Id,
            Title = promotion.Title,
            Code = promotion.Code,
            PriceDeduction = promotion.PriceDeduction,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            PromoStatus = promotion.PromoStatus // Add PromoStatus to the VM
        };

        return View(vm);
    }

    // POST: Admin/DiscountUpdate
    [HttpPost]
    public IActionResult AdminDiscountUpdate(PromotionInsertVM vm)
    {
        if (ModelState.IsValid)
        {
            var promotion = db.Promotion.Find(vm.Id);

            if (promotion != null)
            {
                promotion.Title = vm.Title;
                promotion.Code = vm.Code;
                promotion.PriceDeduction = vm.PriceDeduction;
                promotion.StartDate = vm.StartDate;
                promotion.EndDate = vm.EndDate;
                promotion.PromoStatus = vm.PromoStatus; // Update PromoStatus

                db.SaveChanges();
            }

            return RedirectToAction("AdminDiscount");
        }

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminReport(DateTime? startDate, DateTime? endDate)
    {
        // If no start or end date is provided, use the current month and year
        if (!startDate.HasValue || !endDate.HasValue)
        {
            startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            endDate = startDate.Value.AddMonths(1).AddDays(-1);  // Set to the last day of the current month
        }

        // Make sure the endDate includes the whole day by setting it to 23:59:59
        endDate = endDate?.AddHours(23).AddMinutes(59).AddSeconds(59);

        // Get the sales data for the selected date range
        var salesData = GetSalesDataGroupedByDate(startDate.Value, endDate.Value);

        // Calculate total sales for the selected month and year without filtering by Status
        var totalSalesThisMonth = db.Purchase
            .Where(p => p.PaymentDateTime.Month == startDate.Value.Month && p.PaymentDateTime.Year == startDate.Value.Year)
            .Sum(p => p.Amount);

        var totalSalesThisYear = db.Purchase
            .Where(p => p.PaymentDateTime.Year == startDate.Value.Year)
            .Sum(p => p.Amount);

        // Get the best-selling attractions
        var bestSellingAttractions = GetBestSellingAttractions(startDate.Value, endDate.Value);

        // Pass the filtered data to the view
        ViewBag.Labels = salesData.Labels;
        ViewBag.Data = salesData.Data;
        ViewBag.TotalSalesThisMonth = totalSalesThisMonth;
        ViewBag.TotalSalesThisYear = totalSalesThisYear;
        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

        // Pass the best-selling attractions to the view
        ViewBag.BestSellingAttractionsLabels = bestSellingAttractions.Select(a => a.Name).ToList();
        ViewBag.BestSellingAttractionsData = bestSellingAttractions.Select(a => a.SalesCount).ToList();

        return View();
    }

    // Helper function to fetch sales data grouped by date
    private (List<string> Labels, List<decimal> Data) GetSalesDataGroupedByDate(DateTime startDate, DateTime endDate)
    {
        var groupedData = db.Purchase
            .Where(p => p.PaymentDateTime >= startDate && p.PaymentDateTime <= endDate) // Filter by date range
            .GroupBy(p => p.PaymentDateTime.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(p => p.Amount)
            })
            .OrderBy(g => g.Date)
            .ToList();

        var labels = groupedData.Select(g => g.Date.ToString("yyyy-MM-dd")).ToList();
        var data = groupedData.Select(g => g.TotalAmount).ToList();

        return (labels, data);
    }

    // Helper function to fetch best-selling attractions
    private List<BestSellingAttraction> GetBestSellingAttractions(DateTime startDate, DateTime endDate)
    {
        var bestSellingAttractions = db.PurchaseItem
            .Where(pi => pi.Purchase.PaymentDateTime >= startDate && pi.Purchase.PaymentDateTime <= endDate)
            .GroupBy(pi => pi.Ticket.Attraction.Name)
            .Select(g => new BestSellingAttraction
            {
                Name = g.Key,
                SalesCount = g.Sum(pi => pi.Quantity)
            })
            .OrderByDescending(a => a.SalesCount)  // Sort by sales count in descending order
            .Take(5)  // Get the top 5 attractions
            .ToList();

        return bestSellingAttractions;
    }
    public class BestSellingAttraction
    {
        public string Name { get; set; }
        public int SalesCount { get; set; }
    }
    public IActionResult DownloadSalesReport()
    {
        // Fetch data from the database
        var salesData = db.Purchase
            .Select(p => new
            {
                Date = p.PaymentDateTime.Date,
                Amount = p.Amount
            })
            .OrderBy(p => p.Date)
            .ToList();

        // Create a new Excel workbook
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Sales Report");

            // Add headers
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Sales";

            // Insert data into the worksheet
            for (int i = 0; i < salesData.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = salesData[i].Date.ToString("yyyy-MM-dd");
                worksheet.Cell(i + 2, 2).Value = salesData[i].Amount;
            }

            // Auto adjust columns
            worksheet.Columns().AdjustToContents();

            // Save the workbook to a memory stream
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Seek(0, SeekOrigin.Begin);

                // Return the Excel file as a downloadable file
                return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "SalesReport.xlsx");
            }

        }

    }
    //============================================ Report End =================================================================
    //============================================ Member Maintenance =========================================================

    // Generate ID for create account
    private string NextRegisterId()
    {
        string max = db.User.Max(s => s.Id) ?? "U000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'U'000");
    }

    // GET Admin/CreateAccount
    [Authorize(Roles = "Admin")]
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

        if (ModelState.IsValid)
        {

            db.Admins.Add(new()
            {
                Id = NextRegisterId(),
                Name = vm.Name,
                Email = vm.Email,
                Password = hp.HashPassword(vm.Password),
                IC = vm.IC,
                PhoneNumber = vm.PhoneNumber,
                Gender = vm.Gender,
                Freeze = false,
            });
            db.SaveChanges();
            TempData["Info"] = "Create successfully. Please login.";
            return RedirectToAction("MemberList");

        }

        return View(vm);
    }

    //GET Admin/MemberList
    [Authorize(Roles = "Admin")]
    public IActionResult MemberList(int MemberPage = 1, int AdminPage = 1, string MemberSearch = "", string AdminSearch = "")
    {
        // Ensure the page numbers are at least 1
        if (MemberPage < 1) MemberPage = 1;
        if (AdminPage < 1) AdminPage = 1;

        // Filter and paginate members
        var membersQuery = db.User.OfType<Member>().AsQueryable(); //convert all "Member" into queryable object allowing filtering
        if (!string.IsNullOrEmpty(MemberSearch))
        {
            membersQuery = membersQuery.Where(m =>
                m.Name.Contains(MemberSearch) ||  //Match Member name contains the search string
                m.Email.Contains(MemberSearch) || //Match Member email 
                m.PhoneNumber.Contains(MemberSearch)); //Match Member Phone number
        }
        var members = membersQuery.ToPagedList(MemberPage, 5); //convert filtered memberQuery into paginated list, MemberPage specifies the number of member for current page

        // Filter and paginate admins
        var adminsQuery = db.User.OfType<Admin>().AsQueryable();
        if (!string.IsNullOrEmpty(AdminSearch))
        {
            adminsQuery = adminsQuery.Where(a =>
                a.Name.Contains(AdminSearch) ||
                a.Email.Contains(AdminSearch) ||
                a.PhoneNumber.Contains(AdminSearch));
        }
        var admins = adminsQuery.ToPagedList(AdminPage, 5);

        // Pass the lists as a tuple to the view
        return View((members, admins));
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
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult AdminUpdateMember()
    {
        var email = User.Identity!.Name; // Get the logged-in user's email
        var m = db.Admins.SingleOrDefault(member => member.Email == email); // Retrieve member details

        if (m == null) return RedirectToAction("MemberList", "Admin");

        var birthDate = Helper.ConvertIcToBirthDate(m.IC);

        var vm = new UpdateProfileVm
        {
            Name = m.Name,
            Email = m.Email,
            PhoneNumber = m.PhoneNumber,
            Gender = m.Gender,
            IC = m.IC,
            BirthDate = birthDate,
        };

        return View(vm);
    }

    //POST Admin/AdminUpdateMember 
    [HttpPost]
    public IActionResult AdminUpdateMember(UpdateProfileVm vm, string? Id)
    {
        // Retrieve the logged-in user's email
        var email = User.Identity!.Name;

        // Find the user by email
        var user = db.Admins.SingleOrDefault(member => member.Email == email);

        if (user == null)
        {
            return RedirectToAction("MemberList", "Admin"); // Redirect if user not found
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

            // Save changes to the database
            db.SaveChanges();

            TempData["Info"] = "Details updated successfully.";
            return RedirectToAction("MemberList", "Admin");
        }

        return View(vm);

    }

    //GET Admin/MemberDetails
    [Authorize(Roles = "Admin")]
    [HttpGet("Admin/MemberDetails/{Id?}")]
    public IActionResult MemberDetails(string? Id)
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

    //GET Admin/AdminChangePassword
    public IActionResult AdminChangePassword()
    {
        return View();
    }

    //POST Admin/AdminChangePassword
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult AdminChangePassword(ChangePassword vm)
    {
        // Retrieve the logged-in user's email
        var email = User.Identity!.Name;

        // Find the user by email
        var user = db.Admins.SingleOrDefault(member => member.Email == email);


        if (user == null)
        {
            return RedirectToAction("MemberList", "Admin"); // Redirect if user not found
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
            return RedirectToAction("MemberList", "Admin");

        }


        return View(vm);
    }

    //============================================ Member Maintenance End =========================================================

    //------------------------------------------
    //Admin purchase
    //------------------------------------------
    public List<Purchase> getAllPurcahse(bool? unpaid,bool? refundpayment)
    {
        var allPurchases = db.Purchase
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Ticket)
                        .ThenInclude(at => at.Attraction)
                .Include(us => us.User)
                .Include(po => po.Promotion)
                .OrderByDescending(p => p.PaymentDateTime)
                .ToList();


        if (unpaid == false)
        {
            allPurchases = allPurchases
            .Where(p => p.Status == "S") // Compare p.Id with Payment.PurchaseId
             .ToList();
        }
        if (refundpayment==true)
        {
            allPurchases = allPurchases
            .Where(p=>p.Status=="M"||p.Status=="R") // Compare p.Id with Payment.PurchaseId
            .OrderBy(p=>p.Status) 
            .ToList();
        }

        return allPurchases;
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminPurchase(string? purchaseID, DateTime? validdate, string? payment,string? refundPayment)
    {

        var allPurchases = getAllPurcahse(false,null);

        IEnumerable<Purchase> purchaseItems;
        //Purcahse fillter
        if (validdate != null)
        {
            purchaseItems = allPurchases
             .Where(pi => pi.PaymentDateTime.Date == validdate?.Date);
        }
        else if (payment != null)
        {
            purchaseItems = getAllPurcahse(true, null);  // If no validdate is provided, show all purchase items(Unpaid)
        }
        else if (!string.IsNullOrEmpty(refundPayment))
        {
            purchaseItems = getAllPurcahse(null, true);
        }
        else
        {
            purchaseItems = allPurchases;  // If no validdate is provided, show all purchase items(Payment)
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

    [Authorize(Roles = "Admin")]
    public ActionResult AdminPurchaseDetail(string purchaseID)
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

    [Authorize(Roles = "Admin")]
    public ActionResult AdminPurchaseTicket(string? purchaseId, DateTime validDate)
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

    [Authorize(Roles = "Admin")]
    public IActionResult AdminPurchaseUpdate(string ticketID, string purcahseItemID)
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

    //----------------------------------------
    //get PurchaseItemsID
    //-----------------------------------------
    private string NextPurchaseItem_Id(int count)
    {
        // TODO
        string max = db.PurchaseItem.Max(t => t.Id) ?? "PI0000";
        int n = int.Parse(max[2..]);
        return (n + count).ToString("'PI'0000");
    }
    [HttpPost]
    public IActionResult AdminPurchase(PurchaseViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var purchaseItems = db.PurchaseItem
           .Where(pi => pi.Id == vm.PurchaseUpdate.Id && pi.TicketId == vm.PurchaseUpdate.TicketId)
           .Include(pi => pi.Ticket)
           .FirstOrDefault();

            if (vm.PurchaseUpdate.Quantity > purchaseItems?.Quantity)
            {
                TempData["SuccessMessage"] = "Ticket updated Cancled (Ticket more than " + purchaseItems?.Quantity + " )!";
                return Redirect("/Admin/AdminPurchase");
            }

            if (purchaseItems?.Quantity == vm.PurchaseUpdate.Quantity)
            {
                purchaseItems.validDate = vm.PurchaseUpdate.validDate;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Ticket updated successfully!";
                return Redirect("/Admin/AdminPurchase");
            }

            if (vm.PurchaseUpdate.Quantity < purchaseItems?.Quantity)
            {
                if (vm.PurchaseUpdate.validDate == purchaseItems?.validDate)
                {
                    TempData["SuccessMessage"] = "Ticket updated Cancled (Same valid date " + vm.PurchaseUpdate.validDate.ToString("yyyy-MM-dd") + " )!";
                    return Redirect("/Admin/AdminPurchase");
                }


                if (purchaseItems?.validDate == vm.PurchaseUpdate.validDate)
                {
                    purchaseItems.Quantity += vm.PurchaseUpdate.Quantity;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Ticket updated successfully!";
                    return Redirect("/Admin/AdminPurchase");
                }

                purchaseItems.Quantity -= vm.PurchaseUpdate.Quantity;
                db.SaveChanges();


                var purcahseID = purchaseItems.PurchaseId;
                var reAdd =
                    new PurchaseItem
                    {
                        PurchaseId = purchaseItems.PurchaseId,
                        Id = NextPurchaseItem_Id(1),
                        validDate = vm.PurchaseUpdate.validDate,
                        TicketId = vm.PurchaseUpdate.TicketId,
                        Quantity = vm.PurchaseUpdate.Quantity,
                    };


                db.PurchaseItem.Add(reAdd);

                db.SaveChanges();
                TempData["SuccessMessage"] = "Ticket updated successfully!";
                return Redirect("/Admin/AdminPurchase");
            }



        }
        return Json(ModelState.IsValid);
    }
    //-------------------------------------
    //change status
    //-------------------------------------
   
    [HttpPost]
    public IActionResult ChangePurchseStatus(string? purchaseID)
    {
        if (purchaseID == null)
        {
            return Json(new { success = false, message = "Purchase ID not provided." });
        }

        var purchaseItems = db.Purchase
            .FirstOrDefault(pi => pi.Id == purchaseID);

        if (purchaseItems == null)
        {
            return Json(new { success = false, message = "Purchase not found!" });
        }

        var payment = db.Payment
            .FirstOrDefault(p => p.PurchaseId == purchaseID);

        if (payment != null)
        {
            purchaseItems.Status = "R"; // Refund
            payment.Status = "R";
            db.SaveChanges();

            return Json(new { success = true, message = "Purchase canceled successfully!" });
        }

        return Json(new { success = false, message = "Try again later!" });
    }
    [HttpGet]
    public IActionResult GenerateInvoice(string? purchaseID)
    {
        var getRecortPurchase = db.Purchase
              .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Ticket)
                        .ThenInclude(at => at.Attraction)
                .Include(us => us.User)
                .Include(po => po.Promotion)
                .Where(p=>p.Id==purchaseID)
                .OrderByDescending(p => p.PaymentDateTime)
                .ToList();

        var purchaseItems = getRecortPurchase
                .SelectMany(p => p.PurchaseItems)
                .ToList();

        // Create a MemoryStream to hold the PDF data
        using (var memoryStream = new MemoryStream())
        {

            // Initialize PDF writer and document
            PdfWriter writer = new PdfWriter(memoryStream);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Add Invoice Header
            document.Add(new Paragraph("INVOICE")
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER));

            // Add Company Information
            document.Add(new Paragraph("Phaethon Miyobi")
                .SetFontSize(12));
            document.Add(new Paragraph("https://localhost:7190/Client/HomePage"));

            // Add Client Information
           document.Add(new Paragraph("\nBill To:").SetFontSize(12));

            var purchase = getRecortPurchase.FirstOrDefault();

            if (purchase?.User != null)
            {
                string userName = purchase.User.Name ?? "N/A";
                string userEmail = purchase.User.Email ?? "N/A";
                string userPhone = purchase.User.PhoneNumber ?? "N/A";
                document.Add(new Paragraph($"{userName}\n{userEmail}\nPhone: {userPhone}"));
            }
            else
            {
                document.Add(new Paragraph("User information is not available."));
            }
            // Add Invoice Table
            Table table = new Table(5, true); // 4 columns  
            Cell headerCell;

            // Add header cells with gray background
            headerCell = new Cell().Add(new Paragraph("Ticket Name"))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            table.AddCell(headerCell);

            headerCell = new Cell().Add(new Paragraph("Available Time"))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            table.AddCell(headerCell);

            headerCell = new Cell().Add(new Paragraph("Quantity"))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            table.AddCell(headerCell);

            headerCell = new Cell().Add(new Paragraph("Unit Price (RM)"))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            table.AddCell(headerCell);

            headerCell = new Cell().Add(new Paragraph("Total (RM)"))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            table.AddCell(headerCell);

            decimal subtotal = 0;
            foreach (var s in purchaseItems) {
                table.AddCell(s.Ticket.ticketName); // Ticket Name
                table.AddCell(s.validDate.ToString("yyyy-MM-dd")); // Available Time
                table.AddCell(s.Quantity.ToString()); // Quantity
                table.AddCell($"RM{s.Ticket.ticketPrice:0.00}"); // Unit Price with formatting
                var total = s.Ticket.ticketPrice * s.Quantity; // Total for this item
                table.AddCell($"RM{total:0.00}"); // Add formatted Total
                subtotal += total; // Add to subtotal
            }
            document.Add(table);


            decimal processFee = 4.9m; 
            decimal grandTotal = subtotal + processFee;
            decimal discount = 0;
            var promotion = getRecortPurchase.FirstOrDefault(p => p.PromotionId != null)?.Promotion.PriceDeduction;

            if (promotion != null)
            {
                discount = grandTotal * promotion.Value;
            }
            // Add Invoice Total
            document.Add(new Paragraph($"\nSubtotal: RM{subtotal:0.00}").SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT));
            document.Add(new Paragraph($"Process Fee : RM{processFee:0.00}").SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT));
            if (discount!=0)
            {
                document.Add(new Paragraph($"Discount : RM{discount:0.00}").SetFontSize(12)
               .SetTextAlignment(TextAlignment.RIGHT));
                grandTotal-=discount;
            }
            document.Add(new Paragraph($"Total: RM{grandTotal:0.00}").SetFontSize(12)
                .SetTextAlignment(TextAlignment.RIGHT));

            // Add Footer
            document.Add(new Paragraph("\nThank you for your purchase!")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("payment successful")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));

            // Close the document
            document.Close();

            // Return the PDF as a FileResult
            return File(memoryStream.ToArray(), "application/pdf", "Invoice.pdf");
        }
    }
}



