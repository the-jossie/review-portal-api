using Microsoft.EntityFrameworkCore;

namespace Restaurant_Review_Api.Data
{
    public class DataContext: DbContext {
        private readonly IConfiguration _config;

        public DataContext(IConfiguration config)
        {
            _config = config;
        }

        public virtual DbSet<User> Users {get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("");
        }

    }
}
