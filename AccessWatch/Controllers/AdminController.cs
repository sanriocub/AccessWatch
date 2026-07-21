using Microsoft.AspNetCore.Mvc;

namespace AccessWatch.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
