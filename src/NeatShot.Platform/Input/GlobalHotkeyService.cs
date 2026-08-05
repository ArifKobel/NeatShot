using NeatShot.Core.Input;
using NeatShot.Core.Settings;
using NeatShot.Platform.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace NeatShot.Platform.Input;

public sealed class GlobalHotkeyService : IHotkeyService, IDisposable
{
    private readonly MessageWindow _window;
    private readonly Dictionary<int, HotkeyAction> _registered = [];

    public GlobalHotkeyService(MessageWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
        _window.AddHandler(HandleMessage);
    }

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public IReadOnlyCollection<HotkeyAction> Apply(IReadOnlyDictionary<HotkeyAction, Hotkey> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        UnregisterAll();

        var failed = new List<HotkeyAction>();
        foreach (var (action, hotkey) in bindings)
        {
            var id = (int)action;
            var modifiers = (HOT_KEY_MODIFIERS)hotkey.Modifiers | HOT_KEY_MODIFIERS.MOD_NOREPEAT;
            if (PInvoke.RegisterHotKey(_window.Handle, id, modifiers, (uint)hotkey.VirtualKey))
            {
                _registered[id] = action;
            }
            else
            {
                failed.Add(action);
            }
        }

        return failed;
    }

    public void Dispose() => UnregisterAll();

    private void UnregisterAll()
    {
        foreach (var id in _registered.Keys)
        {
            _ = PInvoke.UnregisterHotKey(_window.Handle, id);
        }

        _registered.Clear();
    }

    private bool HandleMessage(uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message != PInvoke.WM_HOTKEY || !_registered.TryGetValue((int)wParam.Value, out var action))
        {
            return false;
        }

        HotkeyPressed?.Invoke(this, action);
        return true;
    }
}
