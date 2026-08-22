using System.Threading.Channels;
using Apcloudpms.Application.Interfaces;

namespace Apcloudpms.API.Services;

public sealed class ApprovalNotificationQueue : IApprovalNotificationQueue
{
    private readonly Channel<bool> _signals = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    public void Signal() => _signals.Writer.TryWrite(true);

    public async ValueTask WaitAsync(CancellationToken cancellationToken) =>
        _ = await _signals.Reader.ReadAsync(cancellationToken);
}
