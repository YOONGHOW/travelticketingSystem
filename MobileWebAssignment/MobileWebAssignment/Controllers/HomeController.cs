﻿using Microsoft.AspNetCore.Mvc;

namespace MobileWebAssignment.Controllers
{
    public class HomeController : Controller
    {
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
        

    }
}
