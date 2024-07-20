namespace Jifer.Services.Implementations
{
    using Jifer.Data.Constants;
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using Jifer.Services.Models.Login;
    using Jifer.Services.Models.Profile;
    using Jifer.Services.Models.Register;
    using Microsoft.AspNetCore.Identity;
    using Jifer.Data.Repositories;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
    using System.ComponentModel;
    using System.Reflection;

    public class UserService : IUserService
    {
        private readonly IRepository repository;
        private readonly UserManager<JUser> userManager;
        private readonly SignInManager<JUser> signInManager;
        private readonly IFriendHelperService friendHelper;

        public UserService(
            IRepository _repository,
            UserManager<JUser> _userManager,
            SignInManager<JUser> _signInManager,
            IFriendHelperService _friendHelper)
        {
            repository = _repository;
            userManager = _userManager;
            signInManager = _signInManager;
            friendHelper = _friendHelper;
        }

        private async Task<JUser> GetCurrentUserAsync()
        {
            var userId = signInManager.Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User is not authenticated.");
            }
            return await userManager.FindByIdAsync(userId);
        }

        public async Task RegisterUserAsync(RegisterViewModel model)
        {
            var invite = await repository.All<JInvitation>()
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.InvitationCode.ToString() == model.InviteCode);

            if (invite == null || invite.ExpirationDate < DateTime.Now)
            {
                throw new InvalidOperationException("This invite link is invalid or has expired.");
            }

            var userByUsername = await userManager.FindByNameAsync(model.UserName);
            var userByEmail = await userManager.FindByEmailAsync(model.Email);

            if (userByUsername != null)
            {
                throw new InvalidOperationException("Username is taken.");
            }

            if (userByEmail != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var newUser = new JUser
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

            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                throw new InvalidOperationException(string.Join(", ", errorMessages));
            }

            await userManager.AddToRoleAsync(newUser, "User");

            var sender = await userManager.FindByIdAsync(invite.SenderId);

            invite.Sender= sender;
            invite.ReceiverId = newUser.Id;
            invite.Receiver = newUser;
            newUser.ReceivedJInvitations.Add(invite);

            var friendship = new JShip()
            {
                Sender=invite.Sender,
                Receiver=invite.Receiver,
                SenderId=invite.SenderId,
                ReceiverId=invite.ReceiverId,
                SendDate=DateTime.Now,
                Status=ValidationConstants.FriendshipStatus.Pending
            };


            friendship.InteractionDate = DateTime.Now;
            friendship.Status = ValidationConstants.FriendshipStatus.Confirmed;

            invite.Sender.SentFriendRequests.Add(friendship);
            newUser.ReceivedFriendRequests.Add(friendship);

            await repository.AddAsync(friendship);
            await repository.SaveChangesAsync();
        }

        public async Task LoginUserAsync(LoginViewModel model)
        {
            var user = await userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                throw new InvalidOperationException("Invalid login attempt.");
            }

            var result = await signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Invalid login attempt.");
            }
        }

        public async Task LogoutUserAsync()
        {
            await signInManager.SignOutAsync();
        }

        public async Task<ProfileViewModel> GetProfileAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

            var friends = await friendHelper.GetConfirmedFriendsAsync(user);

            var sentFriendRequests = await repository.All<JShip>()
                .Where(f => f.SenderId == user.Id && f.IsActive)
                .Include(f => f.Receiver)
                .ToListAsync();

            var receivedFriendRequests = await repository.All<JShip>()
                .Where(f => f.ReceiverId == user.Id && f.IsActive)
                .Include(f => f.Sender)
                .ToListAsync();

            var sentInvitations = await repository.All<JInvitation>()
                .Where(i => i.SenderId == user.Id && i.IsActive)
                .Include(i => i.Receiver)
                .ToListAsync();

            var jGos = await repository.All<JGo>()
                .Where(j => j.AuthorId == user.Id && j.IsActive)
                .ToListAsync();

            return new ProfileViewModel
            {
                User = user,
                Friends = friends,
                SentFriendRequests = sentFriendRequests,
                ReceivedFriendRequests = receivedFriendRequests,
                SentInvitations = sentInvitations,
                JGos = jGos
            };
        }

        public async Task<ProfileViewModel> GetOtherProfileAsync(string userId, string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return null;
            }

            if (currentUser == user)
            {
                return await GetProfileAsync(userId);
            }

            var friends = await friendHelper.GetConfirmedFriendsAsync(user);
            var isFriendOfFriends = await friendHelper.IsUserFriendOfFriendsAsync(user, currentUser);

            var receivedFriendRequests = await repository.All<JShip>()
                .Where(f => f.ReceiverId == user.Id && f.IsActive)
                .ToListAsync();

            var sentFriendRequests = await repository.All<JShip>()
                .Where(f => f.SenderId == currentUser.Id && f.IsActive)
                .ToListAsync();

            var isFriendRequestSent = receivedFriendRequests.Any(fr => fr.SenderId == currentUser.Id && fr.Status == ValidationConstants.FriendshipStatus.Pending);
            var hasPendingInvitation = sentFriendRequests.Any(fr => fr.SenderId == user.Id && fr.Status == ValidationConstants.FriendshipStatus.Pending);

            var profileModel = new ProfileViewModel
            {
                User = user,
                IsFriendRequestSent = isFriendRequestSent,
                HasPendingInvitation = hasPendingInvitation,
                IsFriendOfFriend = isFriendOfFriends
            };

            var friendship = await repository.All<JShip>()
                .FirstOrDefaultAsync(f => (f.SenderId == user.Id || f.SenderId == currentUser.Id)
                    && (f.ReceiverId == currentUser.Id || f.ReceiverId == user.Id)
                    && f.Status == ValidationConstants.FriendshipStatus.Confirmed
                    && f.IsActive);

            if (friendship != null)
            {
                profileModel.IsFriend = true;
            }

            if ((user.Accessibility == ValidationConstants.Accessibility.FriendsOnly && !friends.Contains(currentUser))
                || (user.Accessibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly && (!isFriendOfFriends && !friends.Contains(currentUser))))
            {
                return profileModel;
            }

            profileModel.Friends = friends;
            profileModel.JGos = await repository.All<JGo>()
                .Where(j => j.AuthorId == user.Id && j.IsActive)
                .ToListAsync();

            return profileModel;
        }

        public async Task SendFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = new JShip()
            {
                SenderId = currentUser.Id,
                ReceiverId = user.Id,
                SendDate = DateTime.Now,
                Status = ValidationConstants.FriendshipStatus.Pending
            };

            currentUser.SentFriendRequests.Add(friendshipReq);
            user.ReceivedFriendRequests.Add(friendshipReq);

            await repository.AddAsync(friendshipReq);
            await repository.SaveChangesAsync();
        }

        public async Task CancelFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();

            var user = await userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = await repository.All<JShip>()
                .FirstOrDefaultAsync(f => f.SenderId == currentUser.Id && f.ReceiverId == user.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return;
            }

            if (friendshipReq.Status == ValidationConstants.FriendshipStatus.Pending)
            {
                friendshipReq.WithdrawnDate = DateTime.Now;
                friendshipReq.WithdrawnBy = currentUser;
                friendshipReq.Status = ValidationConstants.FriendshipStatus.Withdrawn;
            }

            await repository.SaveChangesAsync();
        }

        public async Task AcceptFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = await repository.All<JShip>()
                .FirstOrDefaultAsync(f => f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return;
            }

            friendshipReq.InteractionDate = DateTime.Now;
            friendshipReq.Status = ValidationConstants.FriendshipStatus.Confirmed;

            await repository.SaveChangesAsync();
        }

        public async Task DeclineFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = await repository.All<JShip>()
                .FirstOrDefaultAsync(f => f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return;
            }

            if (friendshipReq.Status == ValidationConstants.FriendshipStatus.Pending)
            {
                friendshipReq.InteractionDate = DateTime.Now;
                friendshipReq.Status = ValidationConstants.FriendshipStatus.Rejected;
            }

            await repository.SaveChangesAsync();
        }

        public async Task RemoveFriendshipAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendship = await repository.All<JShip>()
                .FirstOrDefaultAsync(f => (f.SenderId == user.Id || f.SenderId == currentUser.Id)
                    && (f.ReceiverId == currentUser.Id || f.ReceiverId == user.Id)
                    && f.Status == ValidationConstants.FriendshipStatus.Confirmed
                    && f.IsActive);

            if (friendship == null)
            {
                return;
            }

            friendship.IsActive = false;

            await repository.SaveChangesAsync();
        }
    }
}
