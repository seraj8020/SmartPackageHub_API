using HubManagement.Application.Intake.DTOs;
using MediatR;
using SmartPackageHub.Applications.Abstractions.Commands;

namespace HubManagement.Application.Intake.Commands
{
    /// <summary>
    /// Processes offline staff intake queues by confirming each parcel with the
    /// existing single-intake pipeline (idempotent via clientRequestId).
    /// </summary>
    public sealed class BatchIntakeCommandHandler
        : CreateCommandHandler<BatchIntakeCommand, IntakeStatusResult<BatchIntakeResponse>>
    {
        public const int MaxBatchSize = 50;

        private readonly IMediator _mediator;

        public BatchIntakeCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<IntakeStatusResult<BatchIntakeResponse>> Handle(
            BatchIntakeCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var items = request.Request?.Items ?? new List<ConfirmIntakeRequest>();
            if (items.Count == 0)
            {
                return new IntakeStatusResult<BatchIntakeResponse>(
                    400,
                    "At least one intake item is required",
                    null);
            }

            if (items.Count > MaxBatchSize)
            {
                return new IntakeStatusResult<BatchIntakeResponse>(
                    400,
                    $"Batch size exceeds the maximum of {MaxBatchSize}",
                    null);
            }

            var response = new BatchIntakeResponse
            {
                Total = items.Count,
            };

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var payload = item ?? new ConfirmIntakeRequest();
                try
                {
                    var result = await _mediator
                        .Send(new ConfirmIntakeCommand(payload), cancellationToken)
                        .ConfigureAwait(false);

                    var succeeded = result.StatusCode >= 200 && result.StatusCode < 300 && result.Body != null;
                    response.Results.Add(new BatchIntakeItemResult
                    {
                        ClientRequestId = payload.ClientRequestId,
                        Awb = result.Body?.Awb ?? payload.Awb ?? payload.Barcode,
                        Succeeded = succeeded,
                        StatusCode = result.StatusCode,
                        Error = result.Error,
                        Intake = result.Body,
                    });

                    if (succeeded)
                        response.Succeeded++;
                    else
                        response.Failed++;
                }
                catch (Exception ex)
                {
                    // Keep the batch alive so one bad parcel does not 500 the whole flush.
                    response.Failed++;
                    response.Results.Add(new BatchIntakeItemResult
                    {
                        ClientRequestId = payload.ClientRequestId,
                        Awb = payload.Awb ?? payload.Barcode,
                        Succeeded = false,
                        StatusCode = 500,
                        Error = string.IsNullOrWhiteSpace(ex.Message)
                            ? "Intake failed unexpectedly"
                            : ex.Message,
                        Intake = null,
                    });
                }
            }

            // Partial success still returns 200 with per-item status codes.
            return new IntakeStatusResult<BatchIntakeResponse>(200, null, response);
        }
    }
}
