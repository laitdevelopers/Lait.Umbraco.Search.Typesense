using global::Typesense;

using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Services
{
    public interface ITypesenseSchemaBuilder
    {
        /// <summary>
        /// Builds the explicit Typesense field list for an index definition: the built-in record
        /// fields plus one field per selected property, typed from the property's editor. A ".*"
        /// auto field is appended so anything a custom record builder adds is still indexed.
        /// </summary>
        IReadOnlyList<Field> BuildFields(IEnumerable<ContentData> contentData);
    }
}
