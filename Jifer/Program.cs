namespace Jifer
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Jifer.Data;
    using Jifer.Data.Models;
    using Jifer.Data.Repositories;
    using Jifer.Services.Implementations;
    using Jifer.Services.Interfaces;
    using Jifer.Data.Configurations;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var environment = builder.Environment.EnvironmentName;
            if (environment == "Development")
            {
                builder.Configuration
                    .AddJsonFile("development.json", optional: true, reloadOnChange: true);
            }
            else
            {
                builder.Configuration
                    .AddJsonFile("production.json", optional: true, reloadOnChange: true);
            }

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<JiferDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<JUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<JiferDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/User/Login";
                options.LogoutPath = "/User/Logout";
            });

            builder.Services.AddScoped<IRepository, Repository>();
            builder.Services.AddScoped<IHomeService, HomeService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IInviteGeneratorService, InviteGeneratorService>();
            builder.Services.AddScoped<IInviteService, InviteService>();
            builder.Services.AddScoped<IFriendHelperService, FriendHelperService>();
            builder.Services.AddScoped<IPostService, PostService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<JUser>>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IRepository>();

                if (app.Environment.IsDevelopment())
                {
                    await DbSeeder.SeedDevelopmentDataAsync(userRepository, userManager);
                }
                else
                {
                    await DbSeeder.SeedProductionDataAsync(userRepository, userManager);
                }
            }


            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            app.Run();
        }

    }
}
