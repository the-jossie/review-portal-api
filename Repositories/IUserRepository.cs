using Restaurant_Review_Api.Models;

namespace Api_Tutorial.Data
{
    public interface IUserRepository
    {
        public bool SaveChanges();

        public void AddEntity<T>(T entity);

        public void RemoveEntity<T>(T entity);

        public IEnumerable<User> GetUsers();

        public User GetUser(int userId);
    }
}
