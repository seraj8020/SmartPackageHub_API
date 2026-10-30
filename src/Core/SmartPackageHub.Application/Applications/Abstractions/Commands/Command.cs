using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract record Command : CommandBase<Unit>;
}
