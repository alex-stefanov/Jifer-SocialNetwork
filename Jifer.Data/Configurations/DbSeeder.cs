namespace Jifer.Data.Configurations
{
    using Jifer.Data.Constants;
    using Jifer.Data.Models;
    using Jifer.Data.Repositories;
    using Microsoft.AspNetCore.Identity;

    public static class DbSeeder
    {
        public static async Task SeedDevelopmentDataAsync(IRepository repository, UserManager<JUser> userManager)
        {
            if (await repository.AnyUsersAsync())
            {
                return;
            }

            var admin = new JUser
            {
                FirstName = "Admin",
                MiddleName = "Adminov",
                LastName = "Adminev",
                Email = "jiferbuisness@gmail.com",
                UserName = "admin",
                Accessibility = ValidationConstants.Accessibility.FriendsOnly,
                Gender = ValidationConstants.ProfileGender.Other,
                DateOfBirth = new DateTime(2000, 5, 16),
                IsActive = true
            };

            var stenli = new JUser
            {
                FirstName = "Stenli",
                LastName = "Kirkov",
                Email = "stenliQk1@gmail.com",
                UserName = "Stenli11",
                Accessibility = ValidationConstants.Accessibility.FriendsOnly,
                Gender = ValidationConstants.ProfileGender.Male,
                DateOfBirth = new DateTime(2000, 1, 26),
                IsActive = true
            };

            var miro = new JUser
            {
                FirstName = "Miro",
                LastName = "Petrov",
                Email = "miro_gotin@gmail.com",
                UserName = "Miroo00",
                Accessibility = ValidationConstants.Accessibility.FriendsOfFriendsOnly,
                Gender = ValidationConstants.ProfileGender.Other,
                DateOfBirth = new DateTime(1990, 1, 1),
                IsActive = true
            };

            var petq = new JUser
            {
                FirstName = "Petq",
                LastName = "Mitokova",
                Email = "petq_hot1@gmail.com",
                UserName = "Petq98",
                Accessibility = ValidationConstants.Accessibility.Public,
                Gender = ValidationConstants.ProfileGender.Female,
                DateOfBirth = new DateTime(1998, 6, 12),
                IsActive = true
            };

            await userManager.CreateAsync(admin, "Admin123!");

            await userManager.CreateAsync(stenli, "Stenli123!");

            await userManager.CreateAsync(miro, "Miro123!");

            await userManager.CreateAsync(petq, "Petq123!");

            await repository.SaveChangesAsync();

            var friendships = new HashSet<JShip>();

            var friendShip1 = new JShip()
            {
                SendDate = new DateTime(2024, 7, 23, 5, 39, 20),
                SenderId = stenli.Id,
                ReceiverId = petq.Id,
                Status = ValidationConstants.FriendshipStatus.Confirmed,
                IsActive=true
            };
            friendships.Add(friendShip1);

            var friendShip2 = new JShip()
            {
                SendDate = new DateTime(2024, 7, 23, 4, 39, 20),
                SenderId = stenli.Id,
                ReceiverId = miro.Id,
                Status = ValidationConstants.FriendshipStatus.Pending,
                IsActive = true
            };
            friendships.Add(friendShip2);

            await repository.AddRangeAsync(friendships);

            var jgos = new HashSet<JGo>();

            var jgo1 = new JGo()
            {
                PublishDate = new DateTime(2024, 7, 23, 5, 30, 10),
                AuthorId=petq.Id,
                Text="Здравейте аз съм нова тук!",
                Visibility=ValidationConstants.Accessibility.Public
            };
            jgos.Add(jgo1);

            var jgo2 = new JGo()
            {
                PublishDate = new DateTime(2024, 7, 23, 5, 40, 10),
                AuthorId = stenli.Id,
                Text = "Здрасти искаш ли да сме приятели?",
                Visibility = ValidationConstants.Accessibility.Public
            };
            jgos.Add(jgo2);

            await repository.AddRangeAsync(jgos);

            await repository.SaveChangesAsync();
        }

        public static async Task SeedProductionDataAsync(IRepository repository, UserManager<JUser> userManager)
        {
            if (await repository.AnyUsersAsync())
            {
                return;
            }

            var admin = new JUser
            {
                FirstName = "Admin",
                MiddleName = "Adminov",
                LastName = "Adminev",
                Email = "jiferbuisness@gmail.com",
                UserName = "admin",
                Accessibility = ValidationConstants.Accessibility.FriendsOnly,
                Gender = ValidationConstants.ProfileGender.Other,
                DateOfBirth = new DateTime(2000, 5, 16),
                IsActive = true
            };

            var baseUser = new JUser
            {
                FirstName = "Alex",
                MiddleName = "Ivailov",
                LastName = "Stefanov",
                Email = "rlgalexbgto@gmail.com",
                UserName = "alexx00",
                Accessibility = ValidationConstants.Accessibility.Public,
                Gender = ValidationConstants.ProfileGender.Male,
                DateOfBirth = new DateTime(2007, 5, 16),
                IsActive = true
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.CreateAsync(baseUser, "Alex123!");
        }
    }
}
