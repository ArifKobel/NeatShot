using NeatShot.Core.Settings;

namespace NeatShot.Core.Input;

public interface IHotkeyService
{
    event EventHandler<HotkeyAction>? HotkeyPressed;

    IReadOnlyCollection<HotkeyAction> Apply(IReadOnlyDictionary<HotkeyAction, Hotkey> bindings);
}
