using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseGeolocationProvider
    {
        Task<List<GeolocationEntity>> GetGeolocationAsync(IContent content);
    }
}
