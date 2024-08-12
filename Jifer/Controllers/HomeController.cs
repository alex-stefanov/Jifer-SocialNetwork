namespace Jifer.Controllers
{
    using Jifer.Services.Interfaces;
    using Jifer.Services.Models;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics;

    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            this._homeService = homeService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("CreateRoles", "Role");
        }

        public async Task<IActionResult> Welcome()
        {
            var user = await _homeService.GetCurrentUserAsync(User);

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