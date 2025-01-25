using Restaurant_Review_Api.Data;

namespace Restaurant_Review_Api.Repositories
{
    public interface IRepositoryBase
    {
        public bool SaveChanges();

        public void AddEntity<T>(T entity);

        public void RemoveEntity<T>(T entity);
    }

    public class RepositoryBase
    {
        DataContext _entityFramework;

        public RepositoryBase(IConfiguration config)
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
    }
}
