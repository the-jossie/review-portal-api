using Microsoft.EntityFrameworkCore;
using Ca_Bank_Api.Models;

namespace Ca_Bank_Api.Data
{
    public class DataContext: DbContext {
        private readonly IConfiguration _config;

        public DataContext(IConfiguration config)
        {
            _config = config;
        }

        public virtual DbSet<User> Users {get; set;}
        public virtual DbSet<Auth> AuthUsers {get; set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_config.GetConnectionString("DefaultConnection"), optionsBuilder => optionsBuilder.EnableRetryOnFailure());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("CaBankSchema");

            modelBuilder.Entity<User>().ToTable("Users", "CaBankSchema")
            .HasKey(u => u.UserId);

            modelBuilder.Entity<Auth>().ToTable("Auth", "CaBankSchema").HasKey(u => u.Email);
        }

    }
}
