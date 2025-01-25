using Restaurant_Review_Api.Models;

namespace Restaurant_Review_Api.Repositories
{
    public interface IUserRepository
    {
        public bool SaveChanges();

        public void AddEntity<T>(T entity);

        public void RemoveEntity<T>(T entity);

        public IEnumerable<User> GetAllUsers();

        public User GetUser(int userId);
    }
}
