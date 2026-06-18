using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Cms.Integrations.Search.Typesense.Controllers
{
    [VersionedApiBackOfficeRoute(Constants.ApiRouteSegment)]
    [ApiExplorerSettings(GroupName = Constants.ApiName)]
    [MapToApi(Constants.ApiName)]
    [Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
    public abstract class TypesenseControllerBase : ManagementApiControllerBase
    {
    }
}
