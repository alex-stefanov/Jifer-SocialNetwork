namespace Jifer.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Jifer.Services.Models.Register;
    using Jifer.Services.Models.Login;
    using Jifer.Services.Interfaces;

    public class UserController : Controller
    {
        private readonly IUserService _userService;

        private readonly IHomeService _homeService;

        public UserController(IUserService userService,
            IHomeService homeService)
        {
            this._userService = userService;
            this._homeService = homeService;

        }

        [HttpGet]
        public IActionResult Register(string code)
        {
            var model = new RegisterViewModel() { InviteCode=code };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _userService.RegisterUserAsync(model);
            }
            catch (InvalidOperationException ex)
            {

                if (ex.Message== "Този покана линк е невалиден или е изтекъл.")
                {
                    ModelState.AddModelError("FirstName", ex.Message);
                }else if (ex.Message== "Потребителското име е заето.")
                {
                    ModelState.AddModelError("UserName", ex.Message);
                }else if (ex.Message== "Вече има потребител с този имейл.")
                {
                    ModelState.AddModelError("Email", ex.Message);
                }
                else
                {
                    ModelState.AddModelError("FirstName", "Възникна проблем, свържете се с нас и/или опитайте отново");
                }

                return View(model);
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
            return RedirectToAction("Welcome", "Home");
        }

        [HttpGet]
        public IActionResult Login()
        {
            var model = new LoginViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _userService.LoginUserAsync(model);

                return RedirectToAction("Welcome", "Home");
            }
            catch (InvalidOperationException)
            {
                ViewData["LoginFailed"] = true;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutConfirmed()
        {
            await _userService.LogoutUserAsync();
            return RedirectToAction("Login", "User");
        }

        public async Task<IActionResult> ViewProfile()
        {
            try
            {
                var user = await _homeService.GetCurrentUserAsync(User);

                var model = await _userService.GetProfileAsync(user.Id);

                return View(model);
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> ViewOtherProfile(string otherId)
        {
            var user = await _homeService.GetCurrentUserAsync(User);

            try
            {
                var model = await _userService.GetOtherProfileAsync(user.Id, otherId);

                return View(model);
            }
            catch (Exception ex)
            {
                if(ex.Message== "Same user")
                {
                    return RedirectToAction("ViewProfile", new { user.Id });
                }

                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> SendFriendShip(string otherId)
        {
            try
            {
                await _userService.SendFriendRequestAsync(otherId);

                return RedirectToAction("ViewOtherProfile", new { otherId });
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> CancelFriendRequest(string otherId)
        {
            try
            {
                await _userService.CancelFriendRequestAsync(otherId);
                return RedirectToAction("ViewOtherProfile", new { otherId });
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> AcceptJShip(string otherId)
        {
            try
            {
                await _userService.AcceptFriendRequestAsync(otherId);
                return RedirectToAction("ViewOtherProfile", new { otherId });
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> DeclineJShip(string otherId)
        {
            try
            {
                await _userService.DeclineFriendRequestAsync(otherId);
                return RedirectToAction("ViewOtherProfile", new { otherId });
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> RemoveJShip(string otherId)
        {
            try
            {
                await _userService.RemoveFriendshipAsync(otherId);
                return RedirectToAction("ViewProfile");
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
