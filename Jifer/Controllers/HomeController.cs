namespace Jifer.Controllers
{
    using Jifer.Services.Interfaces;
    using Jifer.Services.Models;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics;

    public class HomeController : Controller
    {
        private readonly IHomeService homeService;

        public HomeController(IHomeService _homeService)
        {
            this.homeService = _homeService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("CreateRoles", "Role");
        }

        public async Task<IActionResult> Welcome()
        {
            var user = await homeService.GetCurrentUserAsync(User);

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