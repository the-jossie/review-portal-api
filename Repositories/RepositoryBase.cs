using Ca_Bank_Api.Data;

namespace Ca_Bank_Api.Repositories
{
    public interface IRepositoryBase
    {
        public bool SaveChanges();

        public void AddEntity<T>(T entity);

        public void UpdateEntity<T>(T entity);
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

        public void UpdateEntity<T>(T entityToUpdate)
        {
            if (entityToUpdate != null)
            {
                _entityFramework.Update(entityToUpdate);
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
