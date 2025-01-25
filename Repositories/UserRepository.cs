using Restaurant_Review_Api.Data;
using Restaurant_Review_Api.Models;

namespace Restaurant_Review_Api.Repositories
{
    public class UserRepository
    {
        DataContext _entityFramework;

        public UserRepository(IConfiguration config)
        {
            _entityFramework = new DataContext(config);
        }

        public bool SaveChanges()
        {
            return _entityFramework.SaveChanges() > 0;
        }

        public void AddEntity<T>(T entityToAdd)
        {
            if (entityToAdd != null)
            {
                _entityFramework.Add(entityToAdd);
            }
        }

        public void RemoveEntity<T>(T entity)
        {
            if (entity != null)
            {
                _entityFramework.Remove(entity);
            }
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
