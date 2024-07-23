namespace Jifer.Controllers
{
    using Jifer.Services.Interfaces;
    using Jifer.Services.Models.Post;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    public class PostController : Controller
    {
        private readonly IPostService postService;

        private readonly IHomeService homeService;

        public PostController(IPostService _postService,
            IHomeService _homeService)
        {
            postService = _postService;
            homeService = _homeService;
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

            var user = await homeService.GetCurrentUserAsync(User);

            if (user != null)
            {
                await postService.CreateJGoAsync(user, model);

                return RedirectToAction("Welcome", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> JGoPage(int page = 1)
        {
            var currentUser = await homeService.GetCurrentUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var viewModel = await postService.GetJGoPageAsync(currentUser.Id, page);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await homeService.GetCurrentUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var success = await postService.DeletePostAsync(id, currentUser.Id);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction("JGoPage");
        }

        public async Task<IActionResult> MyJGos(int page = 1)
        {
            var user = await homeService.GetCurrentUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var model = await postService.GetMyJGosAsync(user.Id, page);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMyJGo(int id)
        {
            var user = await homeService.GetCurrentUserAsync(User);

            if(user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var userId = user.Id;

            var success = await postService.DeleteMyJGoAsync(id, userId);

            if (!success)
            {
                return RedirectToAction("Error", "Home");
            }

            return RedirectToAction("MyJGos");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMyJGo(int id, string newText)
        {
            var user = await homeService.GetCurrentUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var userId = user.Id;

            var success = await postService.UpdateMyJGoAsync(id, newText, userId);

            if (!success)
            {
                TempData["ErrorMessage"] = "You can only edit posts within 15 minutes of creation.";

                return RedirectToAction("MyJGos");
            }

            return RedirectToAction("MyJGos");
        }

        public async Task<IActionResult> OtherJGos(string otherId, int page = 1)
        {
            var currentUser = await homeService.GetCurrentUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var model = await postService.GetOtherJGosAsync(otherId, currentUser.Id, page);

            if (model == null)
            {
                return RedirectToAction("Error", "Home");
            }

            ViewData["OtherId"] = otherId;

            return View(model);
        }
    }
}
