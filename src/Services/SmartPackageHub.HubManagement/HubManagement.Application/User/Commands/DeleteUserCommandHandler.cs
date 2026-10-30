using HubManagement.Application.Societies.DTOs;
using HubManagement.Core.Hubs.Entities;
using Microsoft.Extensions.Logging;
using SmartPackageHub.Applications.Abstractions.Commands;
using SmartPackageHub.Applications.Abstractions.Repositories;

namespace HubManagement.Application.HubUsers.Commands
{
    public sealed class DeleteUserCommandHandler : UpdateCommandHandler<DeleteUserCommand, DeleteCommandResult>
    {
        private static readonly Action<ILogger, Guid, Exception?> LogUserDeleted =
            LoggerMessage.Define<Guid>(
                LogLevel.Information,
                new EventId(1, nameof(DeleteUserCommandHandler)),
                "Deleted user {UserId}");
        private readonly IRepository<User> _users;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(IRepository<User> users, ILogger<DeleteUserCommandHandler> logger, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _users = users;
            _logger = logger;
        }

        public override async Task<DeleteCommandResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await _users.GetByIdAsync(request.UserId).ConfigureAwait(false);
            if (user == null)
                return new DeleteCommandResult(false);

            _users.Delete(user);
            await UnitOfWork!.CommitAsync(cancellationToken).ConfigureAwait(false);
            LogUserDeleted(_logger, user.Id, null);
            return new DeleteCommandResult(true);
        }
    }
}
