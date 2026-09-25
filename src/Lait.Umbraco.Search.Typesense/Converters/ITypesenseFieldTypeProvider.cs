using global::Typesense;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    /// <summary>
    /// Implemented by an <see cref="ITypesenseIndexValueConverter"/> that knows which Typesense
    /// field type its output serializes to. Collections are created with an explicit schema, so a
    /// converter that declares its type gets a correctly typed field instead of one Typesense has
    /// to infer from the first document it sees.
    /// </summary>
    public interface ITypesenseFieldTypeProvider
    {
        /// <summary>
        /// Gets the Typesense field type produced by <see cref="ITypesenseIndexValueConverter.ParseIndexValues"/>.
        /// </summary>
        FieldType GetFieldType();
    }
}
