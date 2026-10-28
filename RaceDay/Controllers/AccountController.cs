using Microsoft.AspNetCore.Mvc;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    public class AccountController : Controller
    {
        // Display login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Display registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
    }
}
