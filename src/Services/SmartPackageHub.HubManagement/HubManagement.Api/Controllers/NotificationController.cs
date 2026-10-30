using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>
    /// SMS, email, push notifications
    /// </summary>
    [Route("api/v{version:apiVersion}/notification")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        public NotificationController()
        {
        }
    }
}
