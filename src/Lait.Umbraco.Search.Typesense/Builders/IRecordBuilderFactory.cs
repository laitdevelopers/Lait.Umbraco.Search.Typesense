using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Builders
{
    public interface IRecordBuilderFactory
    {
        IRecordBuilder GetRecordBuilderService(IContent content);
    }
}
