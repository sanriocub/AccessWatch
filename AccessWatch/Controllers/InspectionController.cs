using Microsoft.AspNetCore.Mvc;

namespace AccessWatch.Controllers
{
    public class InspectionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
