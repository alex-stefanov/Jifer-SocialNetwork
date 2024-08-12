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

    public class UserService : IUserService
    {
        private readonly IRepository _repository;
        private readonly UserManager<JUser> _userManager;
        private readonly SignInManager<JUser> _signInManager;
        private readonly IFriendHelperService _friendHelper;

        public UserService(
            IRepository repository,
            UserManager<JUser> userManager,
            SignInManager<JUser> signInManager,
            IFriendHelperService friendHelper)
        {
            this._repository = repository;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._friendHelper = friendHelper;
        }

        private async Task<JUser> GetCurrentUserAsync()
        {
            var userId = _signInManager.Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User is not authenticated.");
            }
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task RegisterUserAsync(RegisterViewModel model)
        {
            var invites = _repository.All<JInvitation>().ToList();

            var invite = _repository.All<JInvitation>()
                .FirstOrDefault(i => i.InvitationCode.ToString() == model.InviteCode);

            if (invite == null || invite.ExpirationDate < DateTime.Now)
            {
                throw new InvalidOperationException("Този покана линк е невалиден или е изтекъл.");
            }

            var userByUsername = _repository.AllReadonly<JUser>()
                .FirstOrDefault(u => u.UserName == model.UserName);

            var userByEmail = _repository.AllReadonly<JUser>()
                .FirstOrDefault(u => u.Email == model.Email);

            if (userByUsername != null)
            {
                throw new InvalidOperationException("Потребителското име е заето.");
            }

            if (userByEmail != null)
            {
                throw new InvalidOperationException("Вече има потребител с този имейл.");
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

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                throw new InvalidOperationException(string.Join(", ", errorMessages));
            }

            await _userManager.AddToRoleAsync(newUser, "User");

            var sender = await _userManager.FindByIdAsync(invite.SenderId);

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

            await _repository.AddAsync(friendship);
            await _repository.SaveChangesAsync();
        }

        public async Task LoginUserAsync(LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                throw new InvalidOperationException("Invalid login attempt.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Invalid login attempt.");
            }
        }

        public async Task LogoutUserAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<ProfileViewModel> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

            var friends = await _friendHelper.GetConfirmedFriendsAsync(user);

            var sentFriendRequests = await _repository.All<JShip>()
                .Where(f => f.SenderId == user.Id && f.IsActive)
                .Include(f => f.Receiver)
                .ToListAsync();

            var receivedFriendRequests = await _repository.All<JShip>()
                .Where(f => f.ReceiverId == user.Id && f.IsActive)
                .Include(f => f.Sender)
                .ToListAsync();

            var sentInvitations = await _repository.All<JInvitation>()
                .Where(i => i.SenderId == user.Id && i.IsActive)
                .Include(i => i.Receiver)
                .ToListAsync();

            var jGos = await _repository.All<JGo>()
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

            var user = await _userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return null;
            }

            if (currentUser == user)
            {
                throw new Exception("Same user");
            }

            var friends = await _friendHelper.GetConfirmedFriendsAsync(user);

            var isFriendOfFriends = await _friendHelper.IsUserFriendOfFriendsAsync(user, currentUser);

            var receivedFriendRequests = await _repository.All<JShip>()
                .Where(f => f.ReceiverId == user.Id && f.IsActive)
                .ToListAsync();

            var sentFriendRequests = await _repository.All<JShip>()
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

            var friendship = await _repository.All<JShip>()
                .FirstOrDefaultAsync(f => (f.SenderId == user.Id || f.SenderId == currentUser.Id)
                    && (f.ReceiverId == currentUser.Id || f.ReceiverId == user.Id)
                    && f.Status == ValidationConstants.FriendshipStatus.Confirmed
                    && f.IsActive);

            if (friendship != null)
            {
                profileModel.IsFriend = true;
            }

            if ((user.Accessibility == ValidationConstants.Accessibility.FriendsOnly && friends.FirstOrDefault(f => f.Id == currentUser.Id) == null)
                || (user.Accessibility == ValidationConstants.Accessibility.FriendsOfFriendsOnly && (isFriendOfFriends==false && friends.FirstOrDefault(f => f.Id == currentUser.Id) == null)))
            {

                return profileModel;
            }

            profileModel.Friends = friends;
            profileModel.JGos = await _repository.All<JGo>()
                .Where(j => j.AuthorId == user.Id && j.IsActive)
                .ToListAsync();

            return profileModel;
        }

        public async Task SendFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();

            var user = await _userManager.FindByIdAsync(otherId);

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

            await _repository.AddAsync(friendshipReq);
            await _repository.SaveChangesAsync();
        }

        public async Task CancelFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();

            var user = await _userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = await _repository.All<JShip>()
                .FirstOrDefaultAsync(f => f.SenderId == currentUser.Id && f.ReceiverId == user.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return;
            }

            if (friendshipReq.Status == ValidationConstants.FriendshipStatus.Pending)
            {
                friendshipReq.WithdrawnDate = DateTime.Now;
                friendshipReq.WithdrawnById = currentUser.Id;
                friendshipReq.WithdrawnBy = currentUser;
                friendshipReq.Status = ValidationConstants.FriendshipStatus.Withdrawn;
                friendshipReq.IsActive = false;
            }

            await _repository.SaveChangesAsync();
        }

        public async Task AcceptFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await _userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = await _repository.All<JShip>()
                .FirstOrDefaultAsync(f => f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return;
            }

            friendshipReq.InteractionDate = DateTime.Now;
            friendshipReq.Status = ValidationConstants.FriendshipStatus.Confirmed;

            await _repository.SaveChangesAsync();
        }

        public async Task DeclineFriendRequestAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await _userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendshipReq = await _repository.All<JShip>()
                .FirstOrDefaultAsync(f => f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == ValidationConstants.FriendshipStatus.Pending && f.IsActive);

            if (friendshipReq == null)
            {
                return;
            }

            if (friendshipReq.Status == ValidationConstants.FriendshipStatus.Pending)
            {
                friendshipReq.InteractionDate = DateTime.Now;
                friendshipReq.Status = ValidationConstants.FriendshipStatus.Rejected;
                friendshipReq.IsActive = false;
            }

            await _repository.SaveChangesAsync();
        }

        public async Task RemoveFriendshipAsync(string otherId)
        {
            var currentUser = await GetCurrentUserAsync();
            var user = await _userManager.FindByIdAsync(otherId);

            if (user == null || currentUser == null)
            {
                return;
            }

            var friendship = await _repository.All<JShip>()
                .FirstOrDefaultAsync(f => (f.SenderId == user.Id || f.SenderId == currentUser.Id)
                    && (f.ReceiverId == currentUser.Id || f.ReceiverId == user.Id)
                    && f.Status == ValidationConstants.FriendshipStatus.Confirmed
                    && f.IsActive);

            if (friendship == null)
            {
                return;
            }

            friendship.IsActive = false;

            await _repository.SaveChangesAsync();
        }
    }
}
