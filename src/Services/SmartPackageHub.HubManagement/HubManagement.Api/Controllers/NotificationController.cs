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
        readonly IMediator _mediator;
        public NotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }
    }
}
