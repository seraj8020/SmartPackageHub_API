using AutoMapper;
using HubManagement.Application.Intake.DTOs;
using SmartPackageHub.Applications.Abstractions.Queries;

namespace HubManagement.Application.Intake.Queries
{
    

    public sealed class ParseLabelQueryHandler : QueryHandler<ParseLabelQuery, ParsedLabelDto?>
    {
        public ParseLabelQueryHandler(IMapper mapper) : base(mapper)
        {
        }

        protected override Task<ParsedLabelDto?> HandleAsync(ParseLabelQuery request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Text))
                return Task.FromResult<ParsedLabelDto?>(null);

            return Task.FromResult<ParsedLabelDto?>(LabelTextParser.Parse(request.Text));
        }
    }

    

    

    
}
