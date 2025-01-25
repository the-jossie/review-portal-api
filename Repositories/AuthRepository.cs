using Restaurant_Review_Api.Data;
using Restaurant_Review_Api.Models;

namespace Restaurant_Review_Api.Repositories
{
    public interface IAuthRepository : IRepositoryBase
    {
        public IEnumerable<Auth> GetAllUsers();
    }

    public class AuthRepository : RepositoryBase, IAuthRepository
    {
        DataContext _entityFramework;

        public AuthRepository(IConfiguration config) : base(config)
        {
            _entityFramework = new DataContext(config);
        }

        public IEnumerable<Auth> GetAllUsers()
        {
            // IEnumerable<Auth> users = _entityFramework.AuthUsers.ToList<Auth>();
            IEnumerable<Auth> users = [.. _entityFramework.AuthUsers];

            return users;
        }
    }
}
