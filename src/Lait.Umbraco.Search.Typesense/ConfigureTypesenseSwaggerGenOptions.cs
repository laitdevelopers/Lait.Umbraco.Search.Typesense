using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif

using Swashbuckle.AspNetCore.SwaggerGen;

using Umbraco.Cms.Api.Management.OpenApi;

namespace Umbraco.Cms.Integrations.Search.Typesense
{
    /// <summary>
    /// Registers a dedicated Swagger / OpenAPI document for the Typesense Management API so the
    /// backoffice client can be generated and the endpoints appear in the API documentation.
    /// </summary>
    public class ConfigureTypesenseSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            options.SwaggerDoc(
                Constants.ApiName,
                new OpenApiInfo
                {
                    Title = "Umbraco Typesense Search API",
                    Version = "Latest",
                    Description = "Manage Typesense collections (indices) and search from the Umbraco backoffice."
                });

            options.OperationFilter<TypesenseSecurityRequirementsOperationFilter>();
        }
    }

    public class TypesenseSecurityRequirementsOperationFilter : BackOfficeSecurityRequirementsOperationFilterBase
    {
        protected override string ApiName => Constants.ApiName;
    }
}
