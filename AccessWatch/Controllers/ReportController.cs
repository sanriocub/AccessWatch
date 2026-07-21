using Microsoft.AspNetCore.Mvc;

namespace AccessWatch.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
