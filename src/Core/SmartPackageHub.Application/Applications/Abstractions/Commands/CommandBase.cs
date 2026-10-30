using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract record CommandBase<T> : IRequest<T>;
}
