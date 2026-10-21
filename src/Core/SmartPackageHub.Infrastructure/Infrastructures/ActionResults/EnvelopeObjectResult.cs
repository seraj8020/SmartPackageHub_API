using Microsoft.AspNetCore.Mvc;

namespace SmartPackageHub.Infrastructures.ActionResults
{
    public class EnvelopeObjectResult : ObjectResult
    {
        public EnvelopeObjectResult(Envelope envelope)
            : base(envelope ?? throw new ArgumentNullException(nameof(envelope)))
        {
            StatusCode = envelope.Status;
        }
    }
}
