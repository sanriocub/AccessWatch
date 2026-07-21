using Microsoft.AspNetCore.Mvc;

namespace AccessWatch.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
