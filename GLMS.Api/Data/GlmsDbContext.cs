using Microsoft.EntityFrameworkCore;
using GLMS.Api.Models;

namespace GLMS.Api.Data
{
    public class GlmsDbContext : DbContext
    {
        public GlmsDbContext(DbContextOptions<GlmsDbContext> options)
            : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<GLMS.Api.Models.Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Store enums as readable strings
            modelBuilder.Entity<GLMS.Api.Models.Contract>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.Status)
                .HasConversion<string>();
        }
    }
}