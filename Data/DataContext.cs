using Microsoft.EntityFrameworkCore;
using Restaurant_Review_Api.Models;

namespace Restaurant_Review_Api.Data
{
    public class DataContext: DbContext {
        private readonly IConfiguration _config;

        public DataContext(IConfiguration config)
        {
            _config = config;
        }

        public virtual DbSet<User> Users {get; set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_config.GetConnectionString("DefaultConnection"), optionsBuilder => optionsBuilder.EnableRetryOnFailure());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ReviewPortalSchema");

            modelBuilder.Entity<User>().ToTable("Users", "ReviewPortalSchema")
            .HasKey(u => u.UserId);
        }

    }
}
