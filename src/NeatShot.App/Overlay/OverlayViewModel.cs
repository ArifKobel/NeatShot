using CommunityToolkit.Mvvm.ComponentModel;
using NeatShot.Core.Capture;

namespace NeatShot.Overlay;

public sealed partial class OverlayViewModel : ObservableObject
{
    private readonly TaskCompletionSource<PixelRect?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private PixelPoint _anchor;

    public OverlayViewModel(CaptureMode mode, IReadOnlyList<WindowInfo> windows)
    {
        Mode = mode;
        Windows = windows;
    }

    public CaptureMode Mode { get; }

    public IReadOnlyList<WindowInfo> Windows { get; }

    public Task<PixelRect?> Result => _completion.Task;

    [ObservableProperty]
    public partial PixelPoint Cursor { get; private set; }

    [ObservableProperty]
    public partial PixelRect? Selection { get; private set; }

    [ObservableProperty]
    public partial bool IsDragging { get; private set; }

    [ObservableProperty]
    public partial WindowInfo? HoveredWindow { get; private set; }

    public void MoveCursor(PixelPoint position)
    {
        Cursor = position;
        if (IsDragging)
        {
            Selection = PixelRect.FromPoints(_anchor, position);
        }
        else
        {
            HoveredWindow = Windows.FirstOrDefault(window => window.Bounds.Contains(position));
        }
    }

    public void BeginDrag(PixelPoint position)
    {
        _anchor = position;
        Cursor = position;
        IsDragging = true;
        Selection = null;
    }

    public void EndDrag(PixelPoint position)
    {
        if (!IsDragging)
        {
            return;
        }

        IsDragging = false;
        var region = PixelRect.FromPoints(_anchor, position);
        if (!region.IsEmpty)
        {
            Complete(region);
        }
        else if (HoveredWindow is { } window)
        {
            Complete(window.Bounds);
        }
        else
        {
            Selection = null;
        }
    }

    public void Confirm()
    {
        if (Selection is { IsEmpty: false } selection)
        {
            Complete(selection);
        }
    }

    public void Cancel() => _completion.TrySetResult(null);

    private void Complete(PixelRect region)
    {
        Selection = region;
        _completion.TrySetResult(region);
    }
}
