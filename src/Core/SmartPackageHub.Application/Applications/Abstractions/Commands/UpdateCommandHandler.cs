using SmartPackageHub.Applications.Abstractions.Repositories;
using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Commands
{
    public abstract class UpdateCommandHandler<TCommand, TResponse> : CommandHandler, IRequestHandler<TCommand, TResponse> where TCommand : IUpdateCommand<TResponse>
    {
        protected UpdateCommandHandler(IUnitOfWork? unitOfWork = null) : base(unitOfWork)
        {
        }

        public abstract Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken);
    }
}
