namespace Jifer.Controllers
{
    using Jifer.Services.Interfaces;
    using Jifer.Services.Models.Post;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    public class PostController : Controller
    {
        private readonly IPostService _postService;

        private readonly IHomeService _homeService;

        public PostController(IPostService postService,
            IHomeService homeService)
        {
            this._postService = postService;
            this._homeService = homeService;
        }

        [HttpGet]
        public IActionResult CreateJGo()
        {
            var model = new JGoViewModel();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateJGo(JGoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _homeService.GetCurrentUserAsync(User);

            if (user != null)
            {
                await _postService.CreateJGoAsync(user, model);

                return RedirectToAction("Welcome", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> JGoPage(int page = 1)
        {
            var currentUser = await _homeService.GetCurrentUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var viewModel = await _postService.GetJGoPageAsync(currentUser.Id, page);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _homeService.GetCurrentUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var success = await _postService.DeletePostAsync(id, currentUser.Id);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction("JGoPage");
        }

        public async Task<IActionResult> MyJGos(int page = 1)
        {
            var user = await _homeService.GetCurrentUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var model = await _postService.GetMyJGosAsync(user.Id, page);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMyJGo(int id)
        {
            var user = await _homeService.GetCurrentUserAsync(User);

            if(user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var userId = user.Id;

            var success = await _postService.DeleteMyJGoAsync(id, userId);

            if (!success)
            {
                return RedirectToAction("Error", "Home");
            }

            return RedirectToAction("MyJGos");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMyJGo(int id, string newText)
        {
            var user = await _homeService.GetCurrentUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var userId = user.Id;

            var success = await _postService.UpdateMyJGoAsync(id, newText, userId);

            if (!success)
            {
                TempData["ErrorMessage"] = "You can only edit posts within 15 minutes of creation.";

                return RedirectToAction("MyJGos");
            }

            return RedirectToAction("MyJGos");
        }

        public async Task<IActionResult> OtherJGos(string otherId, int page = 1)
        {
            var currentUser = await _homeService.GetCurrentUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var model = await _postService.GetOtherJGosAsync(otherId, currentUser.Id, page);

            if (model == null)
            {
                return RedirectToAction("Error", "Home");
            }

            ViewData["OtherId"] = otherId;

            return View(model);
        }
    }
}
