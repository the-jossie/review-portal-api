using Ca_Bank_Api.Data;
using Ca_Bank_Api.Models;

namespace Ca_Bank_Api.Repositories
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
