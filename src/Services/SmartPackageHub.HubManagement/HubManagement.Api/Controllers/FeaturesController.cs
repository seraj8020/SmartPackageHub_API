using SmartPackageHub.Common.Services.FeatureFlags.Abstractions;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>
    /// Exposes appsettings FeatureFlags so the UI can show/hide features without a rebuild.
    /// </summary>
    [Route("api/v{version:apiVersion}/features")]
    [ApiController]
    public sealed class FeaturesController : ControllerBase
    {
        private readonly IFeatureFlags _featureFlags;

        public FeaturesController(IFeatureFlags featureFlags)
        {
            _featureFlags = featureFlags;
        }

        [HttpGet]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(IReadOnlyDictionary<string, bool>), StatusCodes.Status200OK)]
        public ActionResult<IReadOnlyDictionary<string, bool>> GetAll() =>
            Ok(_featureFlags.GetAll());

        [HttpGet("{featureName}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult GetOne(string featureName) =>
            Ok(new { feature = featureName, enabled = _featureFlags.IsEnabled(featureName) });
    }
}
