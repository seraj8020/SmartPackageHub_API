using SmartPackageHub.Applications.Abstractions.Repositories;
using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract class CommandHandler
    {
        protected readonly IUnitOfWork? UnitOfWork;
        protected CommandHandler(IUnitOfWork? unitOfWork = null)
        {
            UnitOfWork = unitOfWork;
        }
    }

    public abstract class CommandHandler<TCommand> : CommandHandler, IRequestHandler<TCommand, Unit> where TCommand : Command
    {
        protected CommandHandler(IUnitOfWork? unitOfWork = null) : base(unitOfWork)
        {

        }

        public async Task<Unit> Handle(TCommand request, CancellationToken cancellationToken)
        {
            await HandleAsync(request);
            return Unit.Value;
        }

        protected abstract Task HandleAsync(TCommand request);
    }

    public abstract class UpdateCommandHandler<TCommand, TResponse> : CommandHandler, IRequestHandler<TCommand, TResponse> where TCommand : IUpdateCommand<TResponse>
    {
        protected UpdateCommandHandler(IUnitOfWork? unitOfWork = null) : base(unitOfWork)
        {

        }

        public abstract Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken);

    }

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
            return await HandleAsync(request);
        }

        protected abstract Task<Guid> HandleAsync(TCommand request);
    }

   
}
