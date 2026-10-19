using System.Threading.Channels;

namespace HubManagement.Application.Intake
{
    public sealed class IntakeNotificationQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        public void Enqueue(Guid intakeId) => _channel.Writer.TryWrite(intakeId);

        public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
