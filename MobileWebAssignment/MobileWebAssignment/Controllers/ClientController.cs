using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileWebAssignment.Models;

namespace MobileWebAssignment.Controllers
{
    public class ClientController : Controller
    {

        private readonly DB db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientController(DB db, IHttpContextAccessor _httpContextAccessor)
        {
            this.db = db;
            this._httpContextAccessor = _httpContextAccessor;
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
            return View();
        }

        public IActionResult ClientAttractionDetail()
        {
            return View();
        }

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
