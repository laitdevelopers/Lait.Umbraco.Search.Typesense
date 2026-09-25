namespace Umbraco.Cms.Integrations.Search.Typesense.Models
{
    /// <summary>
    /// Strongly-typed representation of a single Umbraco content node to be indexed in Typesense.
    /// Built by <see cref="Builders.ContentRecordBuilder"/> and flattened to a document via
    /// <see cref="ToDocument"/> before being sent to Typesense. Typesense collections in this
    /// integration are created with an auto-schema wildcard field, so any flattened key is indexed.
    /// </summary>
    public class TypesenseRecord
    {
        public TypesenseRecord()
        {
            Data = new Dictionary<string, object>();
            GeolocationData = new List<GeolocationEntity>();
        }

        public TypesenseRecord(TypesenseRecord record)
        {
            ContentTypeAlias = record.ContentTypeAlias;
            Id = record.Id;
            ContentId = record.ContentId;
            Name = record.Name;
            CreateDate = record.CreateDate;
            CreateDateTimestamp = record.CreateDateTimestamp;
            CreatorName = record.CreatorName;
            UpdateDate = record.UpdateDate;
            UpdateDateTimestamp = record.UpdateDateTimestamp;
            WriterName = record.WriterName;
            TemplateId = record.TemplateId;
            Level = record.Level;
            Path = record.Path;
            Url = record.Url;
            Data = record.Data;
            GeolocationData = record.GeolocationData;
        }

        /// <summary>
        /// Typesense document identifier (string). Maps to the Umbraco content <c>Key</c> (GUID).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Numeric Umbraco content node id.
        /// </summary>
        public int ContentId { get; set; }

        public string Name { get; set; }

        public string CreateDate { get; set; }

        /// <summary>
        /// Unix timestamp (seconds).
        /// </summary>
        public long CreateDateTimestamp { get; set; }

        public string CreatorName { get; set; }

        public string UpdateDate { get; set; }

        /// <summary>
        /// Unix timestamp (seconds).
        /// </summary>
        public long UpdateDateTimestamp { get; set; }

        public string WriterName { get; set; }

        public int TemplateId { get; set; }

        public int Level { get; set; }

        public List<string> Path { get; set; }

        public string ContentTypeAlias { get; set; }

        public string Url { get; set; }

        public List<GeolocationEntity> GeolocationData { get; set; }

        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Flattens the record into a single document dictionary suitable for Typesense
        /// (built-in fields + custom property values at the top level).
        /// </summary>
        public Dictionary<string, object> ToDocument()
        {
            var document = new Dictionary<string, object>
            {
                ["id"] = Id,
                ["contentId"] = ContentId,
                ["createDateTimestamp"] = CreateDateTimestamp,
                ["updateDateTimestamp"] = UpdateDateTimestamp,
                ["templateId"] = TemplateId,
                ["level"] = Level
            };

            // Null values are left out rather than sent as JSON null: every field in the collection
            // schema is optional, and Typesense rejects the whole document when a typed field
            // arrives as null (an unpublished node, for instance, has no url and no writer).
            AddIfNotNull(document, "name", Name);
            AddIfNotNull(document, "createDate", CreateDate);
            AddIfNotNull(document, "creatorName", CreatorName);
            AddIfNotNull(document, "updateDate", UpdateDate);
            AddIfNotNull(document, "writerName", WriterName);
            AddIfNotNull(document, "path", Path);
            AddIfNotNull(document, "contentTypeAlias", ContentTypeAlias);
            AddIfNotNull(document, "url", Url);

            if (GeolocationData != null && GeolocationData.Count > 0)
                document["_geoloc"] = GeolocationData;

            foreach (var item in Data)
            {
                if (item.Key == null) continue;

                AddIfNotNull(document, item.Key, item.Value);
            }

            return document;
        }

        private static void AddIfNotNull(Dictionary<string, object> document, string key, object value)
        {
            if (value == null) return;

            document[key] = value;
        }
    }
}
