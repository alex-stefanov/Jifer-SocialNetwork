namespace Jifer.Services.Implementations
{
    using Jifer.Data.Constants;
    using Jifer.Data.Models;
    using Jifer.Data.Repositories;
    using Jifer.Services.Interfaces;
    using Jifer.Services.Models.Post;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PostService : IPostService
    {
        private readonly IRepository repository;
        private readonly UserManager<JUser> userManager;
        private readonly IFriendHelperService friendHelper;

        public PostService(IRepository _repository,
            UserManager<JUser> _userManager,
            IFriendHelperService _friendHelper)
        {
            repository = _repository;
            userManager = _userManager;
            friendHelper = _friendHelper;
        }

        public async Task<JGo> CreateJGoAsync(JUser user, JGoViewModel model)
        {
            var newJGo = new JGo()
            {
                Author=user,
                AuthorId=user.Id,
                PublishDate=DateTime.Now,
                Text=model.Content,
                Visibility = model.Visibility 
            };

            if (user != null && newJGo != null)
            {
                user.JGos.Add(newJGo);

                await repository.AddAsync(newJGo);

                await repository.SaveChangesAsync();
            }

            return newJGo;
        }

        public async Task<TimelineViewModel> GetJGoPageAsync(string userId, int page = 1)
        {
            const int pageSize = 25;

            var currentUser = await userManager.FindByIdAsync(userId);
            var friends = await friendHelper.GetConfirmedFriendsAsync(currentUser);
            var friendsIds = friends.Select(f => f.Id).ToList();

            var friendsOfFriends = new List<JUser>();
            foreach (var friend in friends)
            {
                var foFriends = await friendHelper.GetConfirmedFriendsAsync(friend);
                friendsOfFriends.AddRange(foFriends);
            }
            var friendsOfFriendsIds = friendsOfFriends.Select(f => f.Id).Distinct().ToList();

            var allUsers = friendsIds.Concat(friendsOfFriendsIds).Concat(new[] { userId }).Distinct().ToList();

            var postsQuery = repository.All<JGo>()
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

            return new TimelineViewModel
            {
                Posts = posts,
                PageIndex = page,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize)
            };
        }

        public async Task<bool> DeletePostAsync(int id, string userId)
        {
            var post = await repository.GetByIdAsync<JGo>(id);

            if (post == null)
            {
                return false;
            }

            if (post.AuthorId != userId)
            {
                return false;
            }

            post.IsActive = false;

            await repository.SaveChangesAsync();

            return true;
        }

        public async Task<MyJGosViewModel> GetMyJGosAsync(string userId, int page = 1)
        {
            const int pageSize = 25;

            var totalJGos = await repository.All<JGo>()
                .Where(j => j.AuthorId == userId && j.IsActive)
                .CountAsync();

            var jgos = await repository.All<JGo>()
                .Where(j => j.AuthorId == userId && j.IsActive)
                .OrderByDescending(j => j.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MyJGosViewModel
            {
                JGos = jgos,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalJGos / (double)pageSize)
            };
        }

        public async Task<bool> DeleteMyJGoAsync(int id, string userId)
        {
            var jgo = await repository.GetByIdAsync<JGo>(id);

            if (jgo == null || !jgo.IsActive || jgo.AuthorId != userId)
            {
                return false;
            }

            jgo.IsActive = false;

            await repository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateMyJGoAsync(int id, string newText, string userId)
        {
            var jgo = await repository.GetByIdAsync<JGo>(id);

            if (jgo == null || !jgo.IsActive || jgo.AuthorId != userId)
            {
                return false;
            }

            if (DateTime.UtcNow.Subtract(jgo.PublishDate).TotalMinutes > 15)
            {
                return false;
            }

            jgo.Text = newText;
            await repository.SaveChangesAsync();

            return true;
        }

        public async Task<MyJGosViewModel> GetOtherJGosAsync(string otherId, string currentUserId, int page = 1)
        {
            const int pageSize = 25;

            var currentUser = await userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(otherId);

            if (user == null)
            {
                return null;
            }

            var friends = await friendHelper.GetConfirmedFriendsAsync(currentUser);
            var friendsIds = friends.Select(f => f.Id).ToList();
            var isFriendOfFriend = await friendHelper.IsUserFriendOfFriendsAsync(user, currentUser);

            var totalVisibleJGos = await repository.All<JGo>()
                .Where(p => p.AuthorId == user.Id && p.IsActive &&
                    (p.Visibility == ValidationConstants.Accessibility.Public ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOnly && friendsIds.Contains(p.AuthorId)) ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly &&
                        (friendsIds.Contains(p.AuthorId) || isFriendOfFriend))))
                .CountAsync();

            var jgos = await repository.All<JGo>()
                .Where(p => p.AuthorId == user.Id && p.IsActive &&
                    (p.Visibility == ValidationConstants.Accessibility.Public ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOnly && friendsIds.Contains(p.AuthorId)) ||
                     (p.Visibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly &&
                        (friendsIds.Contains(p.AuthorId) || isFriendOfFriend))))
                .OrderByDescending(j => j.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MyJGosViewModel
            {
                JGos = jgos,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalVisibleJGos / (double)pageSize)
            };
        }
    }
}
