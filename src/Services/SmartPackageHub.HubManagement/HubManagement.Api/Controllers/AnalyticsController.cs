using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/analytics")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        public AnalyticsController()
        {
        }
    }
}
