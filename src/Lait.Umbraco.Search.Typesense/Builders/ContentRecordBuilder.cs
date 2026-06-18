using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Integrations.Search.Typesense.Models;
using Umbraco.Cms.Integrations.Search.Typesense.Services;
using Umbraco.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Builders
{
    public class ContentRecordBuilder
    {
        private TypesenseRecord _record = new();

        private readonly IUserService _userService;

        private readonly IPublishedUrlProvider _urlProvider;

        private readonly ITypesenseSearchPropertyIndexValueFactory _propertyIndexValueFactory;

        private readonly IRecordBuilderFactory _recordBuilderFactory;

        private readonly IUmbracoContextFactory _umbracoContextFactory;

        private readonly ITypesenseGeolocationProvider _geolocationProvider;

        public ContentRecordBuilder(
            IUserService userService,
            IPublishedUrlProvider urlProvider,
            ITypesenseSearchPropertyIndexValueFactory propertyIndexValueFactory,
            IRecordBuilderFactory recordBuilderFactory,
            IUmbracoContextFactory umbracoContextFactory,
            ITypesenseGeolocationProvider geolocationProvider)
        {
            _userService = userService;

            _urlProvider = urlProvider;

            _propertyIndexValueFactory = propertyIndexValueFactory;

            _recordBuilderFactory = recordBuilderFactory;

            _umbracoContextFactory = umbracoContextFactory;

            _geolocationProvider = geolocationProvider;
        }

        public ContentRecordBuilder BuildFromContent(IContent content, Func<IProperty, bool> filter = null)
        {
            using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

            // Typesense documents require a string "id"; use the content GUID key.
            _record.Id = content.Key.ToString();

            var creator = _userService.GetProfileById(content.CreatorId);
            var writer = _userService.GetProfileById(content.WriterId);

            _record.ContentId = content.Id;
            _record.Name = content.Name;

            _record.CreateDate = content.CreateDate.ToString();
            _record.CreateDateTimestamp = new DateTimeOffset(content.CreateDate).ToUnixTimeSeconds();
            _record.CreatorName = creator?.Name;
            _record.UpdateDate = content.UpdateDate.ToString();
            _record.UpdateDateTimestamp = new DateTimeOffset(content.UpdateDate).ToUnixTimeSeconds();
            _record.WriterName = writer?.Name;

            _record.TemplateId = content.TemplateId.HasValue ? content.TemplateId.Value : -1;
            _record.Level = content.Level;
            _record.Path = content.Path.Split(',').ToList();
            _record.ContentTypeAlias = content.ContentType.Alias;
            _record.Url = _urlProvider.GetUrl(content.Key);
            _record.GeolocationData = _geolocationProvider.GetGeolocationAsync(content).ConfigureAwait(false).GetAwaiter().GetResult();
            _record.Data = new();

            if (content.PublishedCultures.Any())
            {
                foreach (var culture in content.PublishedCultures)
                {
                    _record.Data.Add($"name-{culture}", content.CultureInfos[culture].Name);
                    _record.Data.Add($"url-{culture}", _urlProvider.GetUrl(content.Key, culture: culture));
                }
            }

            foreach (var property in content.Properties.Where(filter ?? (p => true)))
            {
                if (!_record.Data.ContainsKey(property.Alias))
                {
                    if (property.PropertyType.VariesByCulture())
                    {
                        foreach (var culture in content.PublishedCultures)
                        {
                            var indexValue = _propertyIndexValueFactory.GetValue(property, culture);
                            _record.Data.Add($"{indexValue.Key}-{culture}", indexValue.Value);
                        }
                    }
                    else
                    {
                        var indexValue = _propertyIndexValueFactory.GetValue(property, null);
                        _record.Data.Add(indexValue.Key, indexValue.Value);
                    }
                }
            }

            AddCustomValues(_record, content);

            return this;
        }

        public TypesenseRecord Build() => _record;

        protected void SetRecord(TypesenseRecord record)
        {
            _record = record;
        }

        protected virtual ContentRecordBuilder AddCustomValues(TypesenseRecord record, IContent content)
        {
            var recordBuilderService = _recordBuilderFactory.GetRecordBuilderService(content);
            if (recordBuilderService == null)
            {
                return this;
            }

            _record = recordBuilderService.GetRecord(content, record);
            return this;
        }
    }
}
