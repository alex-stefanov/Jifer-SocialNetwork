namespace Jifer.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using Jifer.Data.Models;

    /// <summary>
    /// Database Application Context
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<JUser>
    {
        /// <summary>
        /// Database Application Context Constructor
        /// </summary>
        /// <param name="options">Options for the DbContext</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            
        }

        
    }
}