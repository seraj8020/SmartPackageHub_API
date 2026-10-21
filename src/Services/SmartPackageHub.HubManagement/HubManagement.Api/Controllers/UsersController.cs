using HubManagement.Application.HubUsers.Commands;
using HubManagement.Application.HubUsers.DTOs;
using HubManagement.Application.HubUsers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.Types;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        readonly IMediator _mediator;
        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<UserDetailsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll()
        {
            var users = await _mediator.Send(new GetUsersQuery()).ConfigureAwait(false);
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(Guid id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id)).ConfigureAwait(false);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Create([FromBody] CreateUserRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator.Send(new CreateUserCommand(request.Username, request.Email, request.Role, request.IsAdmin, request.Status)).ConfigureAwait(false);
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.User);
        }

        [HttpPut("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator.Send(new UpdateUserCommand(id, request.Email, request.Role, request.IsAdmin, request.Status)).ConfigureAwait(false);
            if (!result.Found)
                return NotFound();
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.User);
        }

        [HttpDelete("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteUserCommand(id)).ConfigureAwait(false);
            if (!result.Found)
                return NotFound();

            return NoContent();
        }
    }
}
