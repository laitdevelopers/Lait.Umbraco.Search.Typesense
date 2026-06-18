using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Integrations.Search.Typesense.Migrations;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public class TypesenseIndexDefinitionStorage : ITypesenseIndexDefinitionStorage<TypesenseIndex>
    {
        private readonly IScopeProvider _scopeProvider;

        public TypesenseIndexDefinitionStorage(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void AddOrUpdate(TypesenseIndex entity)
        {
            using var scope = _scopeProvider.CreateScope();

            if (entity.Id == 0)
                scope.Database.Insert(entity);
            else
                scope.Database.Update(entity);

            scope.Complete();
        }

        public List<TypesenseIndex> Get()
        {
            using var scope = _scopeProvider.CreateScope();

            var result = scope.Database.Fetch<TypesenseIndex>();

            scope.Complete();

            return result;
        }

        public TypesenseIndex GetById(int id)
        {
            using var scope = _scopeProvider.CreateScope();

            var result = scope.Database.SingleById<TypesenseIndex>(id);

            scope.Complete();

            return result;
        }

        public void Delete(int id)
        {
            using var scope = _scopeProvider.CreateScope();

            var entity = scope.Database.SingleById<TypesenseIndex>(id);

            scope.Database.Delete(entity);

            scope.Complete();
        }
    }
}
