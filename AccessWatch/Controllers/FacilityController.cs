using Microsoft.AspNetCore.Mvc;

namespace AccessWatch.Controllers
{
    public class FacilityController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
