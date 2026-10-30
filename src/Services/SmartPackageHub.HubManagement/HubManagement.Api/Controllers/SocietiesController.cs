using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.Societies.Commands;
using HubManagement.Application.Societies.DTOs;
using HubManagement.Application.Societies.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;
using SmartPackageHub.Infrastructures.ActionResults;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
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
            var societies = await _mediator.Send(new GetSocietiesQuery()).ConfigureAwait(false);
            return Ok(societies);
        }

        [HttpGet("getsocietybyid/{societyid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(SocietyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetSocietyById(Guid societyid)
        {
            var result = await _mediator.Send(new GetSocietyByIdQuery(societyid)).ConfigureAwait(false);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(SocietyDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(Guid id)
        {
            var society = await _mediator.Send(new GetSocietyBySocietyIdQuery(id)).ConfigureAwait(false);
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
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator.Send(new CreateSocietyCommand(request.Name, request.Address, request.ContactEmail, request.Phone)).ConfigureAwait(false);
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
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator.Send(new UpdateSocietyCommand(id, request.Name, request.Address, request.ContactEmail, request.Phone)).ConfigureAwait(false);
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
            var result = await _mediator.Send(new DeleteSocietyCommand(id)).ConfigureAwait(false);
            if (!result.Found)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{societyid:guid}/packages")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<ResidentPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetIntakePackages(Guid societyid)
        {
            var packages = await _mediator.Send(new GetPackagesBySocietyIdQuery(societyid)).ConfigureAwait(false);
            if (packages == null)
                return NotFound();

            return Ok(packages);
        }

        [HttpGet("{societyid:guid}/dispatchedpackages")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<ResidentPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetDispatchedPackages(Guid societyid)
        {
            var packages = await _mediator.Send(new DispatchedPackagesQuery(societyid)).ConfigureAwait(false);
            if (packages == null)
                return NotFound();

            return Ok(packages);
        }

        [HttpGet("{societyid:guid}/deliveredpackages")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<ResidentPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetDeliveredPackages(Guid societyid)
        {
            var packages = await _mediator.Send(new DeliveredPackagesQuery(societyid)).ConfigureAwait(false);
            if (packages == null)
                return NotFound();

            return Ok(packages);
        }
    }
}
