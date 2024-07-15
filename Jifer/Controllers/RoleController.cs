using Jifer.Data;
using Jifer.Data.Constants;
using Jifer.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jifer.Controllers
{
    public class RoleController:Controller
    {
        private readonly ApplicationDbContext context;

        private readonly RoleManager<IdentityRole> roleManager;

        private readonly UserManager<JUser> userManager;

        public RoleController(ApplicationDbContext context,
            RoleManager<IdentityRole> _roleManager,
            UserManager<JUser> _userManager)
        {
            this.context = context;
            this.roleManager = _roleManager;
            this.userManager = _userManager;
        }

        public async Task<IActionResult> CreateRoles()
        {
            bool doesAdminExists = await roleManager.RoleExistsAsync("Admin");
            bool doesUserExists = await roleManager.RoleExistsAsync("User");


            if (!doesAdminExists && !doesUserExists)
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
            return RedirectToAction("SeedUsers");

        }

        public async Task<IActionResult> SeedUsers()
        { 

            if (await roleManager.RoleExistsAsync("Admin") &&
                userManager.GetUsersInRoleAsync("Admin").Result.Count == 0)
            {
                var admin = new JUser("Alex", "Stefanov", "Ivailov")
                {
                    Email = "rlgalexbgto@gmail.com",
                    UserName = "admin",
                    Accessibility = ValidationConstants.Accessibility.FriendsOnly,
                    Gender = ValidationConstants.ProfileGender.Male,
                    DateOfBirth =new DateTime(2007, 5, 16),
                    IsActive = true
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");     

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
            return RedirectToAction("Welcome", "Home");

        }
    }
}
