using SmartPackageHub.Applications.Abstractions.Repositories;
using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract class CommandHandler
    {
        protected IUnitOfWork? UnitOfWork { get; }

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
            await HandleAsync(request).ConfigureAwait(false);
            return Unit.Value;
        }

        protected abstract Task HandleAsync(TCommand request);
    }
}
