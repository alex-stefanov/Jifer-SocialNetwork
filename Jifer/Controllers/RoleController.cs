namespace Jifer.Controllers
{
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;

    public class RoleController : Controller
    {
        private readonly IRoleService roleService;

        public RoleController(IRoleService _roleService)
        {
            roleService = _roleService;
        }

        public async Task<IActionResult> CreateRoles()
        {
            //await roleService.CreateRolesAsync();

            return RedirectToAction("SeedUsers");
        }

        public async Task<IActionResult> SeedUsers()
        {
            //await roleService.SeedUsersAsync();

            return RedirectToAction("Welcome", "Home");
        }
    }
}
