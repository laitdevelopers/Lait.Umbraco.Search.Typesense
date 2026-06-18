using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    /// <summary>
    /// Defines a custom index value converter for a given property editor.
    /// </summary>
    public interface ITypesenseIndexValueConverter
    {
        /// <summary>
        /// Gets the property editor alias this converter handles.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Parses the property's index values into a value suitable for a Typesense document.
        /// </summary>
        object ParseIndexValues(IProperty property);
    }
}
