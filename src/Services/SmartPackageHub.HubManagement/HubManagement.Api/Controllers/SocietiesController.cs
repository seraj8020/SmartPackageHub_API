using HubManagement.Application.Societies.Commands;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Application.Societies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.Types;
using SmartPackageHub.Infrastructures.ActionResults;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/societies")]
    public class SocietiesController : ControllerBase
    {
        readonly IMediator _mediator;
        public SocietiesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<SocietyDetailsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAll()
        {
            var societies = await _mediator.Send(new GetSocietiesQuery());
            return Ok(societies);
        }

        [HttpGet("getsocietybyid/{societyid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [Authorize(Roles = "Accountant")]
        [ProducesResponseType(typeof(SocietyDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetSocietyById(Guid societyid)
        {
            var result = await _mediator.Send(new GetSocietyByIdQuery(societyid));
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(SocietyDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(Guid id)
        {
            var society = await _mediator.Send(new GetSocietyDetailsQuery(id));
            if (society == null)
                return NotFound();

            return Ok(society);
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(SocietyDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] CreateSocietyRequest request)
        {
            var result = await _mediator.Send(new CreateSocietyCommand(request.Name, request.Address, request.ContactEmail, request.Phone));
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.Society);
        }

        [HttpPut("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(SocietyDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateSocietyRequest request)
        {
            var result = await _mediator.Send(new UpdateSocietyCommand(id, request.Name, request.Address, request.ContactEmail, request.Phone));
            if (!result.Found)
                return NotFound();
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.Society);
        }

        [HttpDelete("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteSocietyCommand(id));
            if (!result.Found)
                return NotFound();

            return NoContent();
        }
    }
}
