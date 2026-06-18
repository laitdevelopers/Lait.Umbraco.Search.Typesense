using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public class TypesenseNullGeolocationProvider : ITypesenseGeolocationProvider
    {
        public Task<List<GeolocationEntity>> GetGeolocationAsync(IContent content) => Task.FromResult<List<GeolocationEntity>>(null);
    }
}
