using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AspNetCoreMvcWebSite.Models;

namespace AspNetCoreMvcWebSite.Data;

// IdentityDbContext already contains the Identity tables (users, roles ...).
// I add one more table for our AIImage model.
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AIImage> AIImage { get; set; } = default!;
}
