using Microsoft.EntityFrameworkCore;


using GLMS.Web.Models;

namespace GLMS.Web.Data
{
    public class GlmsDbContext : DbContext
    {
        public GlmsDbContext(DbContextOptions<GlmsDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }

        public DbSet<GLMS.Web.Models.Contract> Contracts { get; set; }

        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GLMS.Web.Models.Contract>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.Status)
                .HasConversion<string>();
        }
    }
}