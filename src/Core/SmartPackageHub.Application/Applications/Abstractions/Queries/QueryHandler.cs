using AutoMapper;
using MediatR;

namespace SmartPackageHub.Applications.Abstractions.Queries
{
    public abstract class QueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : Query<TResponse>
    {
        protected IMapper Mapper { get; }

        protected QueryHandler(IMapper mapper)
        {
            Mapper = mapper;
        }

        public async Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken)
        {
            return await HandleAsync(request).ConfigureAwait(false);
        }

        protected abstract Task<TResponse> HandleAsync(TQuery request);
    }
}
