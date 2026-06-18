using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Integrations.Search.Typesense.Converters;

namespace Umbraco.Cms.Integrations.Search.Typesense.Providers
{
    public class ConverterCollectionBuilder
        : OrderedCollectionBuilderBase<ConverterCollectionBuilder, ConverterCollection, ITypesenseIndexValueConverter>
    {
        protected override ConverterCollectionBuilder This => this;
    }
}
