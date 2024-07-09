using Jifer.Data.Models;
using Jifer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Jifer.Models.Register;
using Jifer.Models.Login;
using Jifer.Models.SendEmail;
using Microsoft.EntityFrameworkCore;
using Jifer.Data.Constants;

namespace Jifer.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext context;

        private readonly UserManager<JUser> userManager;

        private readonly SignInManager<JUser> signInManager;

        public UserController(
            ApplicationDbContext _context,
            UserManager<JUser> _userManager,
            SignInManager<JUser> _signInManager)
        {
            context = _context;
            userManager = _userManager;
            signInManager = _signInManager;
        }

        [HttpGet]
        public IActionResult Register(string code)
        {
            var invite = context.Invitations.FirstOrDefault(i => i.InvitationCode.ToString() == code);

            if (invite == null || invite.ExpirationDate < DateTime.Now)
            {
                return Content("This invite link is invalid or has expired.");
            }

            var model = new RegisterViewModel { InviteCode = code };

            return View(model);
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            var invite = context.Invitations.FirstOrDefault(i => i.InvitationCode.ToString() == model.InviteCode);

            if (invite == null || invite.ExpirationDate < DateTime.Now)
            {
                return Content("This invite link is invalid or has expired.");
            }

            var newUser = new JUser()
            {
                FirstName= model.FirstName,
                MiddleName=model.MiddleName,
                LastName=model.LastName,
                Email =  model.Email,
                UserName = model.UserName,
                Accessibility = model.Accessibility,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                IsActive = true
            };

            userManager.CreateAsync(newUser, model.Password);

            userManager.AddToRoleAsync(newUser, "User");

            var friendship = new JShip(invite.Sender, newUser);

            friendship.Accept();

            context.FriendShips.Add(friendship);

            context.SaveChanges();

            return Content("Registration successful and friendship established!");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            var model = new LoginViewModel();

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByNameAsync(model.UserName);

            if (user != null)
            {
                var result = await signInManager.PasswordSignInAsync(user, model.Password, false, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Welcome", "Home");
                }
            }
            ModelState.AddModelError("", "Invalid login");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return RedirectToAction("Login", "User");
        }
    }
}
