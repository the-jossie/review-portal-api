using Restaurant_Review_Api.Data;
using Restaurant_Review_Api.Models;

namespace Restaurant_Review_Api.Repositories
{
    public interface IUserRepository : IRepositoryBase
    {
        public IEnumerable<User> GetAllUsers();

        public User GetUser(int userId);
    }

    public class UserRepository : RepositoryBase, IUserRepository
    {
        DataContext _entityFramework;

        public UserRepository(IConfiguration config) : base(config)
        {
            _entityFramework = new DataContext(config);
        }

        public IEnumerable<User> GetAllUsers()
        {
            IEnumerable<User> users = _entityFramework.Users.ToList<User>();

            return users;
        }

        public User GetUser(int userId)
        {
            User? user = _entityFramework.Users.Where(u => u.UserId == userId).FirstOrDefault<User>();

            if (user != null)
            {
                return user;
            }

            throw new Exception("Failed to Get User");
        }

    }
}
