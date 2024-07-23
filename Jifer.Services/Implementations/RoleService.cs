namespace Jifer.Services.Implementations
{
    using Jifer.Data.Constants;
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Identity;

    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<JUser> userManager;

        public RoleService(RoleManager<IdentityRole> _roleManager, 
            UserManager<JUser> _userManager)
        {
            roleManager = _roleManager;
            userManager = _userManager;
        }

        public async Task CreateRolesAsync()
        {
            bool doesAdminExists = await roleManager.RoleExistsAsync("Admin");
            bool doesUserExists = await roleManager.RoleExistsAsync("User");

            if (!doesAdminExists && !doesUserExists)
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
        }

        public async Task SeedUsersAsync()
        {
            if (await roleManager.RoleExistsAsync("Admin") &&
                (await userManager.GetUsersInRoleAsync("Admin")).Count == 0)
            {
                var admin = new JUser
                {
                    FirstName = "Alex",
                    MiddleName = "Stefanov",
                    LastName = "Ivailov",
                    Email = "jiferbuisness@gmail.com",
                    UserName = "admin",
                    Accessibility = ValidationConstants.Accessibility.FriendsOnly,
                    Gender = ValidationConstants.ProfileGender.Male,
                    DateOfBirth = new DateTime(2007, 5, 16),
                    IsActive = true
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
