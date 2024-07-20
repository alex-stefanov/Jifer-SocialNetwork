namespace Jifer.Controllers
{
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;

    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async Task<IActionResult> CreateRoles()
        {
            await _roleService.CreateRolesAsync();

            return RedirectToAction("SeedUsers");
        }

        public async Task<IActionResult> SeedUsers()
        {
            await _roleService.SeedUsersAsync();

            return RedirectToAction("Welcome", "Home");
        }
    }
}
