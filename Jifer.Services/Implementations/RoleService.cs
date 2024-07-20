namespace Jifer.Services.Implementations
{
    using Jifer.Data.Constants;
    using Jifer.Data.Models;
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Identity;

    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<JUser> _userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<JUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task CreateRolesAsync()
        {
            bool doesAdminExists = await _roleManager.RoleExistsAsync("Admin");
            bool doesUserExists = await _roleManager.RoleExistsAsync("User");

            if (!doesAdminExists && !doesUserExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }
        }

        public async Task SeedUsersAsync()
        {
            if (await _roleManager.RoleExistsAsync("Admin") &&
                (await _userManager.GetUsersInRoleAsync("Admin")).Count == 0)
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

                var result = await _userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
