using System.Text.Json;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Integrations.Search.Typesense.Extensions;
using Umbraco.Cms.Integrations.Search.Typesense.Models;
using Umbraco.Extensions;

namespace Umbraco.Cms.Integrations.Search.Typesense.Converters
{
    public class UmbracoMediaPickerConverter : ITypesenseIndexValueConverter
    {
        private const string UdiPrefix = "umb://media/";

        private readonly IMediaService _mediaService;

        public UmbracoMediaPickerConverter(IMediaService mediaService) => _mediaService = mediaService;

        public string Name => Core.Constants.PropertyEditors.Aliases.MediaPicker3;

        public object ParseIndexValues(IProperty property)
        {
            var list = new List<string>();

            if (!property.TryGetPropertyIndexValue(out string value))
            {
                return list;
            }

            if (value.StartsWith(UdiPrefix))
            {
                var guidPart = value.Substring(UdiPrefix.Length);
                if (Guid.TryParse(guidPart, out Guid guid))
                {
                    var mediaItem = _mediaService.GetById(guid);
                    if (mediaItem != null)
                    {
                        list.Add(mediaItem.GetValue("umbracoFile")?.ToString() ?? string.Empty);
                    }
                }
            }
            else
            {
                var inputMedia = JsonSerializer.Deserialize<IEnumerable<MediaItem>>(value);

                if (inputMedia == null) return list;

                foreach (var item in inputMedia)
                {
                    if (item == null || string.IsNullOrEmpty(item.MediaKey)) continue;

                    var mediaItem = _mediaService.GetById(Guid.Parse(item.MediaKey));

                    if (mediaItem == null) continue;

                    list.Add(mediaItem.GetValue("umbracoFile")?.ToString() ?? string.Empty);
                }
            }

            return list;
        }
    }
}
