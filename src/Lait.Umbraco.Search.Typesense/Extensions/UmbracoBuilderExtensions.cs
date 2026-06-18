using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Integrations.Search.Typesense.Converters;
using Umbraco.Cms.Integrations.Search.Typesense.Providers;

namespace Umbraco.Cms.Integrations.Search.Typesense.Extensions
{
    public static class UmbracoBuilderExtensions
    {
        public static IUmbracoBuilder AddTypesenseConverters(this IUmbracoBuilder builder)
        {
            builder.TypesenseConverters()
                .Append<UmbracoMediaPickerConverter>()
                .Append<UmbracoDecimalConverter>()
                .Append<UmbracoIntegerConverter>()
                .Append<UmbracoBooleanConverter>()
                .Append<UmbracoTagsConverter>();

            return builder;
        }

        public static ConverterCollectionBuilder TypesenseConverters(this IUmbracoBuilder builder)
            => builder.WithCollectionBuilder<ConverterCollectionBuilder>();
    }
}
