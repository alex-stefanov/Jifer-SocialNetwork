using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KazanlakEvents.Infrastructure.Identity;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace KazanlakEvents.Web.Controllers.Api.v1;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("api")]
public class AuthApiController(
	UserManager<ApplicationUser> userManager,
	IConfiguration config) : ControllerBase
{
	[HttpPost("login")]
	[AllowAnonymous]
	[EnableRateLimiting("auth")]
	public async Task<IActionResult> Login(
		[FromBody]
		ApiLoginRequest request)
	{
		ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);

		if (user is null)
		{
			return Unauthorized(new { error = "Invalid credentials" });
		}

		bool passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

		if (!passwordValid)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

		var roles = await userManager.GetRolesAsync(user);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
			new(JwtRegisteredClaimNames.Email, user.Email!),
			new(JwtRegisteredClaimNames.UniqueName, user.UserName!)
		};
		claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var expiresAt = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:ExpirationInMinutes"));

		var jwt = new JwtSecurityToken(
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			claims: claims,
			expires: expiresAt,
			signingCredentials: credentials);

		var token = new JwtSecurityTokenHandler().WriteToken(jwt);

		return Ok(new ApiLoginResponse
		{
			Token = token,
			ExpiresAt = expiresAt,
			User = new ApiUserInfo
			{
				Id = user.Id,
				Email = user.Email!,
				UserName = user.UserName!,
				Roles = roles
			}
		});
	}
}
