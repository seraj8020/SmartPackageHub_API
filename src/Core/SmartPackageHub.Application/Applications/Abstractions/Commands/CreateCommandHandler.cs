using SmartPackageHub.Applications.Abstractions.Repositories;
using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract class CreateCommandHandler<TCommand, TResponse> : CommandHandler, IRequestHandler<TCommand, TResponse> where TCommand : ICreateCommand<TResponse>
    {
        protected CreateCommandHandler(IUnitOfWork? unitOfWork = null) : base(unitOfWork)
        {
        }

        public abstract Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken);
    }

    public abstract class CreateCommandHandler<TCommand> : CommandHandler, IRequestHandler<TCommand, Guid> where TCommand : CreateCommand
    {
        protected CreateCommandHandler(IUnitOfWork? unitOfWork = null) : base(unitOfWork)
        {
        }

        public async Task<Guid> Handle(TCommand request, CancellationToken cancellationToken)
        {
            return await HandleAsync(request).ConfigureAwait(false);
        }

        protected abstract Task<Guid> HandleAsync(TCommand request);
    }
}
