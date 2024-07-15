using Jifer.Data.Models;
using Jifer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Jifer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly UserManager<JUser> userManager;

        public HomeController(ILogger<HomeController> logger,
            UserManager<JUser> userManager)
        {
            this.userManager=userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("CreateRoles", "Role");
        }

        public async Task<IActionResult> Welcome()
        {
            var user =await userManager.GetUserAsync(User);
            return View(user);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}