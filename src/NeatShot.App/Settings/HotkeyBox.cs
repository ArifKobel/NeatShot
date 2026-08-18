using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeatShot.Core.Settings;

namespace NeatShot.Settings;

public sealed class HotkeyBox : TextBox
{
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey),
        typeof(Hotkey?),
        typeof(HotkeyBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyChanged));

    public HotkeyBox()
    {
        SetResourceReference(StyleProperty, typeof(TextBox));
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        Cursor = Cursors.Arrow;
        UpdateText();
    }

    public Hotkey? Hotkey
    {
        get => (Hotkey?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        switch (key)
        {
            case Key.Tab:
                e.Handled = false;
                return;
            case Key.Back or Key.Delete:
                Hotkey = null;
                return;
            case Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin:
                return;
        }

        var modifiers = ToModifiers(Keyboard.Modifiers);
        if (modifiers == HotkeyModifiers.None && key is not (>= Key.F1 and <= Key.F24))
        {
            return;
        }

        Hotkey = new Hotkey(modifiers, KeyInterop.VirtualKeyFromKey(key));
    }

    private static HotkeyModifiers ToModifiers(ModifierKeys keys)
    {
        var result = HotkeyModifiers.None;
        if (keys.HasFlag(ModifierKeys.Control))
        {
            result |= HotkeyModifiers.Control;
        }

        if (keys.HasFlag(ModifierKeys.Alt))
        {
            result |= HotkeyModifiers.Alt;
        }

        if (keys.HasFlag(ModifierKeys.Shift))
        {
            result |= HotkeyModifiers.Shift;
        }

        if (keys.HasFlag(ModifierKeys.Windows))
        {
            result |= HotkeyModifiers.Windows;
        }

        return result;
    }

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((HotkeyBox)d).UpdateText();

    private void UpdateText() => Text = Hotkey?.ToString() ?? "Press a shortcut";
}
