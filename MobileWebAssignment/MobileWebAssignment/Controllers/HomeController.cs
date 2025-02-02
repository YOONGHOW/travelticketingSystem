﻿using Microsoft.AspNetCore.Mvc;

namespace MobileWebAssignment.Controllers
{
    public class HomeController : Controller
    {

        private readonly DB db;

        public HomeController(DB db)
        {
            this.db = db;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Homepage", "Client");
        }
    }
}
