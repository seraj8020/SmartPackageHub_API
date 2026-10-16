using MediatR;
namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract record Command : CommandBase<Unit>;
    public abstract record CommandBase<T> : IRequest<T>;
    public abstract record CreateCommand : IRequest<Guid>;
    public interface IUpdateCommand<out TResponse> : IRequest<TResponse> { }
    public interface ICreateCommand<out TResponse> : IRequest<TResponse> { }
}
