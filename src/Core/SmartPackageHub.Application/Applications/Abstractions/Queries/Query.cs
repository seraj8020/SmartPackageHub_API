using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Queries
{
    public abstract record Query<T> : IRequest<T>;
}
