using NeatShot.Core.Settings;

namespace NeatShot.Core.Tests.Settings;

public class HotkeyTests
{
    [Theory]
    [InlineData("Alt+Shift+3", HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x33)]
    [InlineData("alt+f4", HotkeyModifiers.Alt, 0x73)]
    [InlineData("Win + PrintScreen", HotkeyModifiers.Windows, 0x2C)]
    [InlineData("A", HotkeyModifiers.None, 0x41)]
    public void Parse_ReadsModifiersAndKey(string text, HotkeyModifiers modifiers, int key)
    {
        var hotkey = Hotkey.Parse(text);

        Assert.Equal(new Hotkey(modifiers, key), hotkey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Foo+A")]
    [InlineData("Ctrl+F25")]
    [InlineData("Ctrl+Shift")]
    public void TryParse_RejectsInvalidInput(string text)
    {
        Assert.False(Hotkey.TryParse(text, out _));
    }

    [Theory]
    [InlineData("Alt+Shift+3")]
    [InlineData("Alt+F4")]
    [InlineData("Win+PrintScreen")]
    [InlineData("Ctrl+Alt+Shift+Win+Z")]
    public void ToString_RoundTrips(string text)
    {
        Assert.Equal(text, Hotkey.Parse(text).ToString());
    }
}
