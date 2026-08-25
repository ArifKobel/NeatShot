using Microsoft.Extensions.Hosting;

namespace NeatShot.Overlay;

public sealed class OverlayWarmup : IHostedService
{
    private readonly OverlayService _overlay;

    public OverlayWarmup(OverlayService overlay)
    {
        _overlay = overlay;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _overlay.Prepare();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
