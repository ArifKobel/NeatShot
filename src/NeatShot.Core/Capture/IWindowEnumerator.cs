namespace NeatShot.Core.Capture;

public interface IWindowEnumerator
{
    IReadOnlyList<WindowInfo> GetVisibleWindows();
}
