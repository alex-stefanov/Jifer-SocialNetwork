namespace Jifer
{
    using Microsoft.EntityFrameworkCore;
    using Jifer.Data;
    using Microsoft.Extensions.Configuration;
    using Jifer.Data.Models;
    using Microsoft.AspNetCore.Identity;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Getting connection string from configuration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Add DbContext to DI container
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Setting app services
            builder
                .Services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            builder.Services
                .AddDatabaseDeveloperPageExceptionFilter();

            // Setting Microsoft Identity service
            builder.Services
                .AddDefaultIdentity<JUser>
                (options =>
                {
                    options
                    .SignIn
                    .RequireConfirmedAccount = false;
                    options.Password.RequiredLength = 5;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/User/Login";
            });

            builder.Services.AddControllersWithViews();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            var app = builder.Build();

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