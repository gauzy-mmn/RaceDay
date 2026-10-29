using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Participant")]
    public class ParticipantController : Controller
    {
        public IActionResult Index()
        {
            return Content(
                "Welcome to your RaceDay Participant Dashboard!");
        }
    }
}
