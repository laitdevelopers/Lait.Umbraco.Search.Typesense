namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseIndexDefinitionStorage<T>
        where T : class
    {
        List<T> Get();

        T GetById(int id);

        void AddOrUpdate(T entity);

        void Delete(int id);
    }
}
