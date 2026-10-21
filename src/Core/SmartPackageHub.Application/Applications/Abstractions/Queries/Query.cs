using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Queries
{
#pragma warning disable CA1724 // Query is the application query contract and is qualified by its namespace.
    public abstract record Query<T> : IRequest<T>;
#pragma warning restore CA1724
}
