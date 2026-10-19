using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed record DeleteUserCommand(Guid UserId) : IUpdateCommand<DeleteCommandResult>;

    public sealed class DeleteUserCommandHandler : UpdateCommandHandler<DeleteUserCommand, DeleteCommandResult>
    {
        private readonly IRepository<User> _users;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(IRepository<User> users, ILogger<DeleteUserCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _logger = logger;
        }

        public override async Task<DeleteCommandResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.UserId);
            if (user == null)
                return new DeleteCommandResult(false);

            _users.Delete(user);
            await UnitOfWork!.CommitAsync(cancellationToken);
            _logger.LogInformation("Deleted user {UserId}", user.Id);
            return new DeleteCommandResult(true);
        }
    }
}
