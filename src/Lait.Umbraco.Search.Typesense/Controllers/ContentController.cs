using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Integrations.Search.Typesense.Models;

namespace Umbraco.Cms.Integrations.Search.Typesense.Controllers
{
    /// <summary>
    /// Management API endpoints exposing content types and their properties so the dashboard can
    /// build the index mapping UI.
    /// </summary>
    public class ContentController : TypesenseControllerBase
    {
        private readonly IContentTypeService _contentTypeService;

        public ContentController(IContentTypeService contentTypeService)
        {
            _contentTypeService = contentTypeService;
        }

        [HttpGet("content-types")]
        [ProducesResponseType(typeof(IEnumerable<ContentEntity>), StatusCodes.Status200OK)]
        public IActionResult GetContentTypes()
        {
            var contentTypes = _contentTypeService.GetAll()
                .Select(p => new ContentEntity
                {
                    Alias = p.Alias,
                    Name = p.Name,
                    Icon = p.Icon
                })
                .OrderBy(p => p.Name);

            return Ok(contentTypes);
        }

        [HttpGet("content-types/{alias}/properties")]
        [ProducesResponseType(typeof(IEnumerable<ContentEntity>), StatusCodes.Status200OK)]
        public IActionResult GetProperties(string alias)
        {
            var contentType = _contentTypeService.Get(alias);
            if (contentType == null) return NotFound();

            var properties = contentType.CompositionPropertyTypes
                .Select(p => new ContentEntity
                {
                    Alias = p.Alias,
                    Name = p.Name
                })
                .OrderBy(p => p.Name);

            return Ok(properties);
        }
    }
}
