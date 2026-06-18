using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Integrations.Search.Typesense.Converters;

namespace Umbraco.Cms.Integrations.Search.Typesense.Providers
{
    public class ConverterCollection : BuilderCollectionBase<ITypesenseIndexValueConverter>
    {
        public ConverterCollection(Func<IEnumerable<ITypesenseIndexValueConverter>> items)
            : base(items)
        {
        }
    }
}
