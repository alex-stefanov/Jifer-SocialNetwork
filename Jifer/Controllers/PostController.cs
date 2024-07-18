namespace Jifer.Controllers
{
    using Jifer.Data.Models;
    using Jifer.Data;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Jifer.Models.Post;
    using Jifer.Data.Constants;
    using Microsoft.EntityFrameworkCore;
    using Jifer.Helpers;
    using System.Security.Claims;

    public class PostController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<JUser> userManager;
        private readonly UHelper userHelper;

        public PostController(ApplicationDbContext context,
            UserManager<JUser> userManager,
            UHelper userHelper)
        {
            this.context = context;
            this.userManager = userManager;
            this.userHelper = userHelper;
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

            var user = await userManager.GetUserAsync(User);

            var newJGo = new JGo(user, model.Content) { Visibility = model.Visibility };

            if (user != null && newJGo != null)
            {
                user.JGos.Add(newJGo);

                await context.Posts.AddAsync(newJGo);

                await context.SaveChangesAsync();

                return RedirectToAction("Welcome", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> JGoPage(int page = 1)
        {
            var pageSize = 25;
            var currentUser = await userManager.GetUserAsync(User);
            var userId = currentUser.Id;

            // Get friends and friends of friends
            var friends = await userHelper.GetConfirmedFriendsAsync(currentUser);
            var friendsIds = friends.Select(f => f.Id).ToList();

            // Get friends of friends
            var friendsOfFriends = new List<JUser>();
            foreach (var friend in friends)
            {
                var foFriends = await userHelper.GetConfirmedFriendsAsync(friend);
                friendsOfFriends.AddRange(foFriends);
            }
            var friendsOfFriendsIds = friendsOfFriends.Select(f => f.Id).Distinct().ToList();

            var allUsers = friendsIds.Concat(friendsOfFriendsIds).Concat(new[] { userId }).Distinct().ToList();

            var postsQuery = context.Posts
                .Where(p => allUsers.Contains(p.AuthorId) && p.IsActive)
                .Where(p => p.Visibility == ValidationConstants.Accessibility.Public ||
                            (p.Visibility == ValidationConstants.Accessibility.FriendsOnly && friendsIds.Contains(p.AuthorId)) ||
                            (p.Visibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly && (friendsOfFriendsIds.Contains(p.AuthorId) || friendsIds.Contains(p.AuthorId))) ||
                            p.AuthorId == userId)
                .OrderByDescending(p => p.PublishDate);

            var totalPosts = await postsQuery.CountAsync();
            var posts = await postsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new TimelineViewModel
            {
                Posts = posts,
                PageIndex = page,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            var currentUser = await userManager.GetUserAsync(User);

            if (post.AuthorId != currentUser.Id)
            {
                return Forbid();
            }

            post.IsActive = false;

            await context.SaveChangesAsync();

            return RedirectToAction("JGoPage");
        }

        public async Task<IActionResult> MyJGos(int page = 1)
        {
            const int pageSize = 25; // Number of JGos per page

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var totalJGos = await context.Posts
                .Where(j => j.AuthorId == user.Id && j.IsActive)
                .CountAsync();

            var jgos = await context.Posts
                .Where(j => j.AuthorId == user.Id && j.IsActive)
                .OrderByDescending(j => j.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new MyJGosViewModel
            {
                JGos = jgos,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalJGos / (double)pageSize)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMyJGo(int id)
        {
            var jgo = await context.Posts.FindAsync(id);
            if (jgo == null || !jgo.IsActive || jgo.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return RedirectToAction("Error", "Home");
            }

            jgo.IsActive = false;
            await context.SaveChangesAsync();

            return RedirectToAction("MyJGos");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMyJGo(int id, string newText)
        {
            var jgo = await context.Posts.FindAsync(id);
            if (jgo == null || !jgo.IsActive || jgo.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return RedirectToAction("Error", "Home");
            }

            if (DateTime.UtcNow.Subtract(jgo.PublishDate).TotalMinutes > 15)
            {
                // Return to the view with an error message if update is attempted after 15 minutes
                TempData["ErrorMessage"] = "You can only edit posts within 15 minutes of creation.";
                return RedirectToAction("MyJGos");
            }

            jgo.Text = newText;
            await context.SaveChangesAsync();

            return RedirectToAction("MyJGos");
        }

        public async Task<IActionResult> OtherJGos(string otherId, int page = 1)
        {
            const int pageSize = 25; // Number of JGos per page

            // Get the current user
            var currentUser = await userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            // Find the user whose JGos are being displayed
            var user = await userManager.FindByIdAsync(otherId);
            if (user == null)
            {
                return RedirectToAction("Error", "Home");
            }

            // Get the list of JGos and filter them based on visibility
            var friends = await userHelper.GetConfirmedFriendsAsync(currentUser);
            var friendsIds = friends.Select(f => f.Id).ToList();
            var isFriendOfFriend = await userHelper.IsUserFriendOfFriendsAsync(user, currentUser);

            // Fetch the count of visible posts
            var totalVisibleJGos = await context.Posts
                .Where(p => p.AuthorId == user.Id && p.IsActive &&
                    (p.Visibility == ValidationConstants.Accessibility.Public ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOnly && friendsIds.Contains(p.AuthorId)) ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly &&
                        (friendsIds.Contains(p.AuthorId) || isFriendOfFriend))))
                .CountAsync();

            // Fetch the visible posts with pagination
            var jgos = await context.Posts
                .Where(p => p.AuthorId == user.Id && p.IsActive &&
                    (p.Visibility == ValidationConstants.Accessibility.Public ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOnly && friendsIds.Contains(p.AuthorId)) ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly &&
                        (friendsIds.Contains(p.AuthorId) || isFriendOfFriend))))
                .OrderByDescending(j => j.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new MyJGosViewModel
            {
                JGos = jgos,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalVisibleJGos / (double)pageSize)
            };
            ViewData["OtherId"] = otherId;

            return View(model);
        }





    }
}
