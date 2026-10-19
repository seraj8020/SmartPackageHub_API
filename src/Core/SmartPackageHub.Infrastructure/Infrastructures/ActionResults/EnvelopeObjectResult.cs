using Microsoft.AspNetCore.Mvc;

namespace SmartPackageHub.Infrastructures.ActionResults
{
    public class EnvelopeObjectResult : ObjectResult
    {
        public EnvelopeObjectResult(Envelope envelope)
            : base(envelope)
        {
            StatusCode = envelope.Status;
        }
    }
}
