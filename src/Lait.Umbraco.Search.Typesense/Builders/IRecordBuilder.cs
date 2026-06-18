using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Builders
{
    public interface IRecordBuilder
    {
        TypesenseRecord GetRecord(IContent content, TypesenseRecord record);
    }

    public interface IRecordBuilder<in TContentType>
        : IRecordBuilder where TContentType : IPublishedContent
    {
    }
}
