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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Jifer.Models.Profile;
using Microsoft.AspNetCore.Http.Extensions;
using Jifer.Helpers;
using System.ComponentModel;

namespace Jifer.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext context;

        private readonly UserManager<JUser> userManager;

        private readonly SignInManager<JUser> signInManager;

        private readonly UHelper userHelper;

        public UserController(
            ApplicationDbContext _context,
            UserManager<JUser> _userManager,
            SignInManager<JUser> _signInManager,
            UHelper _userHelper)
        {
            context = _context;
            userManager = _userManager;
            signInManager = _signInManager;
            userHelper = _userHelper;
        }

        [HttpGet]
        public IActionResult Register(string code)
        {
            var invite = context.Invitations.FirstOrDefault(i => i.InvitationCode.ToString() == code);

            if (invite == null || invite.ExpirationDate < DateTime.Now)
            {
                return Content("This invite link is invalid or has expired.");
            }

            var model = new RegisterViewModel();
            TempData["InviteCode"] = code;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (TempData["InviteCode"] != null)
            {
                model.InviteCode = TempData["InviteCode"].ToString();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            
            var invite = context.Invitations.FirstOrDefault(i => i.InvitationCode.ToString() == model.InviteCode);

            if (invite == null || invite.IsExpired())
            {
                return RedirectToAction("Error", "Home");
            }

            var newUser = new JUser()
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.UserName,
                Accessibility = model.Accessibility,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                IsActive = true
            };

            var result = await userManager.CreateAsync(newUser, model.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newUser, "User");
            }

            var sender = await userManager.FindByIdAsync(invite.SenderId);

            invite.ReceiverId = newUser.Id;
            invite.Receiver = newUser;
            newUser.ReceivedJInvitations.Add(invite);
            
            var friendship = new JShip(invite.Sender, newUser);

            friendship.Accept();

            invite.Sender.SentFriendRequests.Add(friendship);
            newUser.ReceivedFriendRequests.Add(friendship);

            await context.FriendShips.AddAsync(friendship);

            await context.SaveChangesAsync();

            return RedirectToAction("Welcome", "Home");
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

            ViewData["LoginFailed"] = true;
            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return RedirectToAction("Login", "User");
        }

        public async Task<IActionResult> ViewProfile()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var friends = await userHelper.GetConfirmedFriendsAsync(user);

            await context.Entry(user)
                .Collection(u => u.SentFriendRequests)
                .Query()
                .Include(f=>f.Receiver)
                .LoadAsync();

            await context.Entry(user)
                .Collection(u => u.ReceivedFriendRequests)
                .Query()
                .Include(f => f.Sender)
                .LoadAsync();

            await context.Entry(user)
                .Collection(u => u.SentJInvitations)
                .Query()
                .Include(i => i.Receiver)
                .LoadAsync();

            await context.Entry(user)
                .Collection(u => u.JGos)
                .LoadAsync();


            var model = new ProfileViewModel()
            {
                User = user,
                Friends = friends,
                SentFriendRequests = user.SentFriendRequests.Where(f=>f.IsActive).ToList(),
                ReceivedFriendRequests = user.ReceivedFriendRequests.Where(f => f.IsActive).ToList(),
                SentInvitations = user.SentJInvitations.Where(i => i.IsActive).ToList(),
                JGos = user.JGos.Where(f => f.IsActive).ToList()
            };

            return View(model);

        }

        public async Task<IActionResult> ViewOtherProfile(string otherId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            var user = await context.Users.FindAsync(otherId);

            if (user == null || currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            if (currentUser == user)
            {
                return RedirectToAction("ViewProfile");
            }

            var friends = await userHelper.GetConfirmedFriendsAsync(user);
            var isFriendOfFriends = await userHelper.IsUserFriendOfFriendsAsync(user, currentUser);

            await context.Entry(currentUser).Collection(u => u.ReceivedFriendRequests).LoadAsync();
            await context.Entry(user).Collection(u => u.ReceivedFriendRequests).LoadAsync();

            var isFriendRequestSent = user.ReceivedFriendRequests.Any(fr => fr.SenderId == currentUser.Id && fr.Status == ValidationConstants.FriendshipStatus.Pending && fr.IsActive);


            var hasPendingInvitation = currentUser.ReceivedFriendRequests.Any(fr => fr.SenderId == user.Id && fr.Status == ValidationConstants.FriendshipStatus.Pending && fr.IsActive);

            var profileModel = new ProfileViewModel()
            {
                User = user,
                IsFriendRequestSent = isFriendRequestSent,
                HasPendingInvitation = hasPendingInvitation
            };

            var friendShip = await context.FriendShips
                     .FirstOrDefaultAsync(f => (f.SenderId == user.Id || f.SenderId == currentUser.Id)
                        && (f.ReceiverId == currentUser.Id || f.ReceiverId == user.Id)
                        && f.Status == ValidationConstants.FriendshipStatus.Confirmed
                        && f.IsActive);

            if (friendShip != null)
            {
                profileModel.IsFriend = true;
            }

            if ((user.Accessibility == ValidationConstants.Accessibility.FriendsOnly && !friends.Contains(currentUser))
                || (user.Accessibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly && (!isFriendOfFriends && !friends.Contains(currentUser))))
            {
                return View(profileModel);
            }

            profileModel.Friends = friends;
            profileModel.JGos = user.JGos;

            return View(profileModel);
        }

        public async Task<IActionResult> SendFriendShip(string otherId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            var user = await context.Users.FindAsync(otherId);

            if (user == null || currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var friendShipReq = new JShip(currentUser, user);

            await context.Entry(currentUser).Collection(u => u.SentFriendRequests).LoadAsync();
            await context.Entry(user).Collection(u => u.ReceivedFriendRequests).LoadAsync();

            currentUser.SentFriendRequests.Add(friendShipReq);
            user.ReceivedFriendRequests.Add(friendShipReq);

            context.FriendShips.Add(friendShipReq);
            await context.SaveChangesAsync();

            return RedirectToAction("ViewOtherProfile", new { otherId = user.Id });
        }

        public async Task<IActionResult> CancelFriendRequest(string otherId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            var user = await context.Users.FindAsync(otherId);

            if (user == null || currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var friendshipReq = await context.FriendShips
                .FirstOrDefaultAsync(f => f.SenderId == currentUser.Id && f.ReceiverId == user.Id && f.Status==ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq==null)
            {
                return RedirectToAction("Error", "Home");
            }

            friendshipReq.Withdraw();

            await context.SaveChangesAsync();

            return RedirectToAction("ViewOtherProfile", new { otherId = user.Id });
        }

        public async Task<IActionResult> AcceptJShip(string otherId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            var user = await context.Users.FindAsync(otherId);

            if (user == null || currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var friendshipReq = await context.FriendShips
                .FirstOrDefaultAsync(f => f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return RedirectToAction("Error", "Home");
            }

            friendshipReq.Accept();

            await context.SaveChangesAsync();

            return RedirectToAction("ViewOtherProfile", new { otherId = user.Id });
        }

        public async Task<IActionResult> DeclineJShip(string otherId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            var user = await context.Users.FindAsync(otherId);

            if (user == null || currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var friendshipReq = await context.FriendShips
                .FirstOrDefaultAsync(f => f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return RedirectToAction("Error", "Home");
            }

            friendshipReq.Reject();

            await context.SaveChangesAsync();

            return RedirectToAction("ViewOtherProfile", new { otherId = user.Id });
        }

        public async Task<IActionResult> RemoveJShip(string otherId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            var user = await context.Users.FindAsync(otherId);

            if (user == null || currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }
            var frinedShips = await context.FriendShips.ToListAsync();
            var friendship = await context.FriendShips
                .FirstOrDefaultAsync(f => (f.SenderId == user.Id || f.SenderId==currentUser.Id)
                && (f.ReceiverId == currentUser.Id || f.ReceiverId==user.Id)
                && f.Status == ValidationConstants.FriendshipStatus.Confirmed
                && f.IsActive);

            if (friendship == null)
            {
                return RedirectToAction("Error", "Home");
            }

            friendship.IsActive = false;

            await context.SaveChangesAsync();

            return RedirectToAction("ViewProfile", new { otherId = user.Id });
        }
    }
}
