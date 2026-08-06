using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeatShot.Platform.Tray;

namespace NeatShot.Tray;

public sealed class TrayController
{
    private readonly TrayIcon _icon;
    private readonly TrayMenuViewModel _viewModel;
    private ContextMenu? _menu;

    public TrayController(TrayIcon icon, TrayMenuViewModel viewModel)
    {
        _icon = icon;
        _viewModel = viewModel;
    }

    public void Show()
    {
        _icon.RightClick += (_, _) => OpenMenu();
        _icon.LeftClick += (_, _) => OpenMenu();
    }

    private void OpenMenu()
    {
        _menu ??= new TrayMenu { DataContext = _viewModel };
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }
}
