using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeatShot.Core.Annotations;
using NeatShot.Imaging;

namespace NeatShot.Editor;

public sealed partial class EditorViewModel : ObservableObject
{
    private const double DefaultFontSize = 24;
    private const double MinimumDragDistance = 2;

    private readonly Func<BitmapSource, string> _save;
    private readonly Func<BitmapSource, string?> _saveAs;
    private ImagePoint _dragStart;
    private Annotation? _dragOrigin;
    private List<ImagePoint>? _strokePoints;

    public EditorViewModel(
        AnnotationDocument document,
        Func<BitmapSource, string> save,
        Func<BitmapSource, string?> saveAs)
    {
        Document = document;
        Bitmap = document.Image.ToBitmapSource();
        Renderer = new AnnotationRenderer(document.Image);
        _save = save;
        _saveAs = saveAs;

        document.Changed += (_, _) => OnPropertyChanged(nameof(Annotations));
        document.HistoryChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
    }

    public event EventHandler? CloseRequested;

    public AnnotationDocument Document { get; }

    public BitmapSource Bitmap { get; }

    public AnnotationRenderer Renderer { get; }

    public IReadOnlyList<Annotation> Annotations => Document.Annotations;

    public IReadOnlyList<Rgba> Palette { get; } =
    [
        Rgba.Red, Rgba.Orange, Rgba.Yellow, Rgba.Green, Rgba.Blue, Rgba.Purple, Rgba.White, Rgba.Black,
    ];

    public IReadOnlyList<double> StrokeWidths { get; } = [2, 4, 6, 10];

    [ObservableProperty]
    public partial EditorTool ActiveTool { get; set; } = EditorTool.Arrow;

    [ObservableProperty]
    public partial Rgba Color { get; set; } = Rgba.Red;

    [ObservableProperty]
    public partial double StrokeWidth { get; set; } = 4;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectionCommand))]
    public partial Annotation? Selected { get; private set; }

    [ObservableProperty]
    public partial Annotation? Preview { get; private set; }

    [ObservableProperty]
    public partial ImagePoint? PendingTextPosition { get; private set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; private set; }

    public double FontSize => DefaultFontSize + StrokeWidth * 2;

    public void PointerDown(ImagePoint point)
    {
        _dragStart = point;
        Preview = null;

        switch (ActiveTool)
        {
            case EditorTool.Select:
                Selected = Document.HitTest(point);
                _dragOrigin = Selected;
                break;
            case EditorTool.Freehand:
                _strokePoints = [point];
                Preview = new FreehandAnnotation(_strokePoints.ToArray(), CurrentStyle);
                break;
            case EditorTool.Counter:
                Commit(new CounterAnnotation(point, Document.NextCounterNumber, CurrentStyle));
                break;
            case EditorTool.Text:
                PendingTextPosition = point;
                break;
        }
    }

    public void PointerMove(ImagePoint point)
    {
        switch (ActiveTool)
        {
            case EditorTool.Select when _dragOrigin is not null:
                Preview = _dragOrigin.Translate(point.X - _dragStart.X, point.Y - _dragStart.Y);
                break;
            case EditorTool.Freehand when _strokePoints is not null:
                _strokePoints.Add(point);
                Preview = new FreehandAnnotation(_strokePoints.ToArray(), CurrentStyle);
                break;
            case EditorTool.Arrow:
            case EditorTool.Rectangle:
            case EditorTool.Ellipse:
            case EditorTool.Highlight:
            case EditorTool.Blur:
            case EditorTool.Pixelate:
                Preview = CreateShape(_dragStart, point);
                break;
        }
    }

    public void PointerUp(ImagePoint point)
    {
        var moved = _dragStart.DistanceTo(point) >= MinimumDragDistance;

        switch (ActiveTool)
        {
            case EditorTool.Select when _dragOrigin is not null && Preview is not null && moved:
                Document.Execute(new ReplaceAnnotationCommand(_dragOrigin, Preview));
                Selected = Preview;
                break;
            case EditorTool.Freehand when _strokePoints is { Count: > 1 }:
                Commit(new FreehandAnnotation(_strokePoints.ToArray(), CurrentStyle));
                break;
            case EditorTool.Arrow:
            case EditorTool.Rectangle:
            case EditorTool.Ellipse:
            case EditorTool.Highlight:
            case EditorTool.Blur:
            case EditorTool.Pixelate:
                if (moved && CreateShape(_dragStart, point) is { } shape)
                {
                    Commit(shape);
                }

                break;
        }

        Preview = null;
        _dragOrigin = null;
        _strokePoints = null;
    }

    public void CommitText(string text, double width, double height)
    {
        if (PendingTextPosition is { } position && !string.IsNullOrWhiteSpace(text))
        {
            var extent = new ImageRect(position.X, position.Y, width, height);
            Commit(new TextAnnotation(position, text.Trim(), CurrentStyle, FontSize, extent));
        }

        PendingTextPosition = null;
    }

    public void CancelText() => PendingTextPosition = null;

    private AnnotationStyle CurrentStyle => new(Color, StrokeWidth);

    private Annotation? CreateShape(ImagePoint from, ImagePoint to)
    {
        var rect = ImageRect.FromPoints(from, to);
        return ActiveTool switch
        {
            EditorTool.Arrow => new ArrowAnnotation(from, to, CurrentStyle),
            EditorTool.Rectangle => new RectangleAnnotation(rect, CurrentStyle),
            EditorTool.Ellipse => new EllipseAnnotation(rect, CurrentStyle),
            EditorTool.Highlight => new HighlightAnnotation(rect, Color),
            EditorTool.Blur => new ObscureAnnotation(rect, ObscureKind.Blur),
            EditorTool.Pixelate => new ObscureAnnotation(rect, ObscureKind.Pixelate),
            _ => null,
        };
    }

    private void Commit(Annotation annotation)
    {
        Document.Execute(new AddAnnotationCommand(annotation));
        Selected = null;
    }

    [RelayCommand]
    private void SelectTool(EditorTool tool)
    {
        ActiveTool = tool;
        if (tool != EditorTool.Select)
        {
            Selected = null;
        }
    }

    [RelayCommand]
    private void SelectColor(Rgba color)
    {
        Color = color;
        Restyle(annotation => annotation switch
        {
            RectangleAnnotation r => r with { Style = r.Style with { Color = color } },
            EllipseAnnotation e => e with { Style = e.Style with { Color = color } },
            ArrowAnnotation a => a with { Style = a.Style with { Color = color } },
            FreehandAnnotation f => f with { Style = f.Style with { Color = color } },
            TextAnnotation t => t with { Style = t.Style with { Color = color } },
            CounterAnnotation c => c with { Style = c.Style with { Color = color } },
            HighlightAnnotation h => h with { Color = color },
            _ => annotation,
        });
    }

    [RelayCommand]
    private void SelectStrokeWidth(double width)
    {
        StrokeWidth = width;
        Restyle(annotation => annotation switch
        {
            RectangleAnnotation r => r with { Style = r.Style with { StrokeWidth = width } },
            EllipseAnnotation e => e with { Style = e.Style with { StrokeWidth = width } },
            ArrowAnnotation a => a with { Style = a.Style with { StrokeWidth = width } },
            FreehandAnnotation f => f with { Style = f.Style with { StrokeWidth = width } },
            _ => annotation,
        });
    }

    private void Restyle(Func<Annotation, Annotation> change)
    {
        if (Selected is null)
        {
            return;
        }

        var updated = change(Selected);
        if (!ReferenceEquals(updated, Selected))
        {
            Document.Execute(new ReplaceAnnotationCommand(Selected, updated));
            Selected = updated;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        Document.Undo();
        Selected = null;
    }

    private bool CanUndo() => Document.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        Document.Redo();
        Selected = null;
    }

    private bool CanRedo() => Document.CanRedo;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelection()
    {
        if (Selected is { } selected)
        {
            Document.Execute(new RemoveAnnotationCommand(selected));
            Selected = null;
        }
    }

    private bool HasSelection() => Selected is not null;

    [RelayCommand]
    private void Copy()
    {
        Export.ClipboardImageService.SetImage(DocumentRenderer.Render(Document));
        StatusMessage = "Copied to clipboard";
    }

    [RelayCommand]
    private void Save()
    {
        var path = _save(DocumentRenderer.Render(Document));
        StatusMessage = $"Saved to {path}";
    }

    [RelayCommand]
    private void SaveAs()
    {
        var path = _saveAs(DocumentRenderer.Render(Document));
        if (path is not null)
        {
            StatusMessage = $"Saved to {path}";
        }
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
