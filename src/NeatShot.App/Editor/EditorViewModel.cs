using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeatShot.Core.Annotations;
using NeatShot.Imaging;

namespace NeatShot.Editor;

public enum Handle
{
    None,
    Body,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
    ArrowStart,
    ArrowEnd,
}

public sealed partial class EditorViewModel : ObservableObject
{
    private const double DefaultFontSize = 22;
    private const double MinimumDragDistance = 2;
    private const double MinimumShapeSize = 2;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 8;

    private readonly Func<BitmapSource, string> _save;
    private readonly Func<BitmapSource, string?> _saveAs;
    private ImagePoint _dragStart;
    private Handle _activeHandle;
    private IReadOnlyList<Annotation> _dragOrigins = [];
    private List<ImagePoint>? _strokePoints;
    private bool _dragging;

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

        document.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(Annotations));
            OnPropertyChanged(nameof(ImageSize));
        };
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

    public string ImageSize => $"{Document.Canvas.Width} × {Document.Canvas.Height}";

    public ImageRect Canvas => Document.CanvasAround(VisibleAnnotations);

    public IEnumerable<Annotation> VisibleAnnotations
    {
        get
        {
            var annotations = Annotations
                .Where(a => a != EditingText)
                .Select(a => Previews.TryGetValue(a.Id, out var preview) ? preview : a);
            return Preview is { } preview ? annotations.Append(preview) : annotations;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsObscureStrength))]
    public partial EditorTool ActiveTool { get; set; } = EditorTool.Arrow;

    [ObservableProperty]
    public partial int ObscureStrength { get; set; } = ObscureAnnotation.DefaultStrength;

    [ObservableProperty]
    public partial Rgba CanvasBackground { get; set; } = new(0x15, 0x15, 0x1A);

    [ObservableProperty]
    public partial Rgba Color { get; set; } = Rgba.Red;

    [ObservableProperty]
    public partial double StrokeWidth { get; set; } = 4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(ShowsObscureStrength))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectionCommand), nameof(DuplicateCommand), nameof(BringToFrontCommand), nameof(SendToBackCommand))]
    public partial IReadOnlyList<Annotation> Selection { get; private set; } = [];

    [ObservableProperty]
    public partial Annotation? Preview { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyDictionary<Guid, Annotation> Previews { get; private set; } = new Dictionary<Guid, Annotation>();

    [ObservableProperty]
    public partial ImageRect? Marquee { get; private set; }

    [ObservableProperty]
    public partial ImagePoint? PendingTextPosition { get; private set; }

    [ObservableProperty]
    public partial TextAnnotation? EditingText { get; private set; }

    [ObservableProperty]
    public partial double Zoom { get; set; } = 1;

    [ObservableProperty]
    public partial bool FitToWindow { get; set; } = true;

    public bool HasSelection => Selection.Count > 0;

    public bool ShowsObscureStrength =>
        ActiveTool is EditorTool.Blur or EditorTool.Pixelate || Selection.Any(a => a is ObscureAnnotation);

    partial void OnObscureStrengthChanged(int value) =>
        Restyle(a => a is ObscureAnnotation obscure ? obscure.WithStrength(value) : a);

    public double FontSize => DefaultFontSize + StrokeWidth * 2;

    public Annotation? PrimarySelection => Selection.Count == 1 ? Selection[0] : null;

    public Handle HitHandle(ImagePoint point, double tolerance)
    {
        if (PrimarySelection is not { } selected)
        {
            return HitAnnotation(point) is not null ? Handle.Body : Handle.None;
        }

        if (selected is ArrowAnnotation arrow)
        {
            if (point.DistanceTo(arrow.Start) <= tolerance)
            {
                return Handle.ArrowStart;
            }

            if (point.DistanceTo(arrow.End) <= tolerance)
            {
                return Handle.ArrowEnd;
            }
        }
        else if (selected.CanResize)
        {
            foreach (var (handle, position) in HandlePositions(selected.Bounds))
            {
                if (point.DistanceTo(position) <= tolerance)
                {
                    return handle;
                }
            }
        }

        return HitAnnotation(point) is not null ? Handle.Body : Handle.None;
    }

    public Annotation? HitAnnotation(ImagePoint point) =>
        Selection.FirstOrDefault(a => a.Bounds.Contains(point)) ?? Document.HitTest(point);

    public static IEnumerable<(Handle Handle, ImagePoint Position)> HandlePositions(ImageRect bounds)
    {
        yield return (Handle.TopLeft, new ImagePoint(bounds.Left, bounds.Top));
        yield return (Handle.Top, new ImagePoint(bounds.Center.X, bounds.Top));
        yield return (Handle.TopRight, new ImagePoint(bounds.Right, bounds.Top));
        yield return (Handle.Right, new ImagePoint(bounds.Right, bounds.Center.Y));
        yield return (Handle.BottomRight, new ImagePoint(bounds.Right, bounds.Bottom));
        yield return (Handle.Bottom, new ImagePoint(bounds.Center.X, bounds.Bottom));
        yield return (Handle.BottomLeft, new ImagePoint(bounds.Left, bounds.Bottom));
        yield return (Handle.Left, new ImagePoint(bounds.Left, bounds.Center.Y));
    }

    public void PointerDown(ImagePoint point, double handleTolerance, bool extendSelection)
    {
        _dragStart = point;
        _dragging = true;
        Preview = null;

        switch (ActiveTool)
        {
            case EditorTool.Select:
                BeginSelectDrag(point, handleTolerance, extendSelection);
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
        if (!_dragging)
        {
            return;
        }

        var dx = point.X - _dragStart.X;
        var dy = point.Y - _dragStart.Y;

        switch (ActiveTool)
        {
            case EditorTool.Select when _activeHandle == Handle.Body:
                Previews = _dragOrigins.ToDictionary(a => a.Id, a => a.Translate(dx, dy));
                break;
            case EditorTool.Select when _activeHandle != Handle.None && _dragOrigins.Count == 1:
                var resized = Resize(_dragOrigins[0], _activeHandle, point);
                Previews = new Dictionary<Guid, Annotation> { [resized.Id] = resized };
                break;
            case EditorTool.Select:
                Marquee = ImageRect.FromPoints(_dragStart, point);
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

    public void PointerUp(ImagePoint point, bool extendSelection)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        var moved = _dragStart.DistanceTo(point) >= MinimumDragDistance;

        switch (ActiveTool)
        {
            case EditorTool.Select:
                EndSelectDrag(moved, extendSelection);
                break;
            case EditorTool.Freehand when _strokePoints is { Count: > 1 }:
                Document.Execute(new AddAnnotationCommand(new FreehandAnnotation(_strokePoints.ToArray(), CurrentStyle)));
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
        Previews = new Dictionary<Guid, Annotation>();
        Marquee = null;
        _dragOrigins = [];
        _activeHandle = Handle.None;
        _strokePoints = null;
    }

    public void SelectAt(ImagePoint point)
    {
        var hit = HitAnnotation(point);
        if (hit is null)
        {
            Selection = [];
        }
        else if (!Selection.Contains(hit))
        {
            Selection = [hit];
        }
    }

    public bool BeginTextEdit(ImagePoint point)
    {
        if (HitAnnotation(point) is not TextAnnotation text)
        {
            return false;
        }

        Selection = [];
        EditingText = text;
        PendingTextPosition = text.Position;
        return true;
    }

    public void CommitText(string text, double width, double height)
    {
        var editing = EditingText;
        EditingText = null;

        if (PendingTextPosition is { } position && !string.IsNullOrWhiteSpace(text))
        {
            var extent = new ImageRect(position.X, position.Y, width, height);
            if (editing is null)
            {
                Commit(new TextAnnotation(position, text.Trim(), CurrentStyle, FontSize, extent));
            }
            else
            {
                var updated = editing with { Text = text.Trim(), Extent = extent };
                Document.Execute(new ReplaceAnnotationCommand(editing, updated));
                ActiveTool = EditorTool.Select;
                Selection = [updated];
            }
        }
        else if (editing is not null)
        {
            Document.Execute(new RemoveAnnotationCommand(editing));
        }

        PendingTextPosition = null;
    }

    public void CancelText()
    {
        EditingText = null;
        PendingTextPosition = null;
    }

    public void ZoomBy(double factor)
    {
        FitToWindow = false;
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
    }

    private AnnotationStyle CurrentStyle => new(Color, StrokeWidth);

    private void BeginSelectDrag(ImagePoint point, double tolerance, bool extend)
    {
        _activeHandle = HitHandle(point, tolerance);

        if (_activeHandle is not Handle.None and not Handle.Body)
        {
            _dragOrigins = [PrimarySelection!];
            return;
        }

        var hit = HitAnnotation(point);
        if (hit is null)
        {
            if (!extend)
            {
                Selection = [];
            }

            _activeHandle = Handle.None;
            return;
        }

        if (extend)
        {
            Selection = Selection.Contains(hit) ? Selection.Where(a => a != hit).ToArray() : [.. Selection, hit];
        }
        else if (!Selection.Contains(hit))
        {
            Selection = [hit];
        }

        _activeHandle = Handle.Body;
        _dragOrigins = Selection;
    }

    private void EndSelectDrag(bool moved, bool extend)
    {
        if (_activeHandle == Handle.None)
        {
            if (Marquee is { IsEmpty: false } marquee)
            {
                var inside = Annotations.Where(a => a.Bounds.IntersectsWith(marquee)).ToArray();
                Selection = extend ? Selection.Union(inside).ToArray() : inside;
            }

            return;
        }

        if (!moved || Previews.Count == 0)
        {
            return;
        }

        var commands = _dragOrigins
            .Where(origin => Previews.ContainsKey(origin.Id))
            .Select(origin => (IEditCommand)new ReplaceAnnotationCommand(origin, Previews[origin.Id]))
            .ToArray();
        Document.Execute(new CompositeCommand(commands));
        Selection = Selection.Select(a => Previews.TryGetValue(a.Id, out var updated) ? updated : a).ToArray();
    }

    private static Annotation Resize(Annotation origin, Handle handle, ImagePoint point)
    {
        if (origin is ArrowAnnotation arrow)
        {
            return handle switch
            {
                Handle.ArrowStart => arrow with { Start = point },
                Handle.ArrowEnd => arrow with { End = point },
                _ => arrow,
            };
        }

        var b = origin.Bounds;
        var left = b.Left;
        var top = b.Top;
        var right = b.Right;
        var bottom = b.Bottom;

        if (handle is Handle.TopLeft or Handle.Left or Handle.BottomLeft)
        {
            left = Math.Min(point.X, right - MinimumShapeSize);
        }

        if (handle is Handle.TopRight or Handle.Right or Handle.BottomRight)
        {
            right = Math.Max(point.X, left + MinimumShapeSize);
        }

        if (handle is Handle.TopLeft or Handle.Top or Handle.TopRight)
        {
            top = Math.Min(point.Y, bottom - MinimumShapeSize);
        }

        if (handle is Handle.BottomLeft or Handle.Bottom or Handle.BottomRight)
        {
            bottom = Math.Max(point.Y, top + MinimumShapeSize);
        }

        return origin.WithBounds(new ImageRect(left, top, right - left, bottom - top));
    }

    private Annotation? CreateShape(ImagePoint from, ImagePoint to)
    {
        var rect = ImageRect.FromPoints(from, to);
        return ActiveTool switch
        {
            EditorTool.Arrow => new ArrowAnnotation(from, to, CurrentStyle),
            EditorTool.Rectangle => new RectangleAnnotation(rect, CurrentStyle),
            EditorTool.Ellipse => new EllipseAnnotation(rect, CurrentStyle),
            EditorTool.Highlight => new HighlightAnnotation(rect, Color),
            EditorTool.Blur => new ObscureAnnotation(rect, ObscureKind.Blur, ObscureStrength),
            EditorTool.Pixelate => new ObscureAnnotation(rect, ObscureKind.Pixelate, ObscureStrength),
            _ => null,
        };
    }

    private void Commit(Annotation annotation)
    {
        Document.Execute(new AddAnnotationCommand(annotation));
        ActiveTool = EditorTool.Select;
        Selection = [annotation];
    }

    private void Restyle(Func<Annotation, Annotation> change)
    {
        if (Selection.Count == 0)
        {
            return;
        }

        var pairs = Selection
            .Select(a => (Before: a, After: change(a)))
            .Where(p => !ReferenceEquals(p.Before, p.After))
            .ToArray();
        if (pairs.Length == 0)
        {
            return;
        }

        Document.Execute(new CompositeCommand(pairs.Select(p => (IEditCommand)new ReplaceAnnotationCommand(p.Before, p.After)).ToArray()));
        Selection = Selection.Select(a => pairs.FirstOrDefault(p => p.Before == a).After ?? a).ToArray();
    }

    [RelayCommand]
    private void SelectTool(EditorTool tool)
    {
        ActiveTool = tool;
        if (tool != EditorTool.Select)
        {
            Selection = [];
        }
    }

    [RelayCommand]
    private void SelectColor(Rgba color)
    {
        Color = color;
        Restyle(a => a.WithColor(color));
    }

    [RelayCommand]
    private void SelectStrokeWidth(double width)
    {
        StrokeWidth = width;
        Restyle(a => a.WithStrokeWidth(width));
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        Document.Undo();
        Selection = [];
    }

    private bool CanUndo() => Document.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        Document.Redo();
        Selection = [];
    }

    private bool CanRedo() => Document.CanRedo;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelection()
    {
        Document.Execute(new CompositeCommand(Selection.Select(a => (IEditCommand)new RemoveAnnotationCommand(a)).ToArray()));
        Selection = [];
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Duplicate()
    {
        var copies = Selection.Select(a => a.Duplicate().Translate(12, 12)).ToArray();
        Document.Execute(new CompositeCommand(copies.Select(a => (IEditCommand)new AddAnnotationCommand(a)).ToArray()));
        Selection = copies;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void BringToFront()
    {
        var commands = Selection.Select(a => (IEditCommand)new ReorderAnnotationCommand(a, Annotations.Count - 1)).ToArray();
        Document.Execute(new CompositeCommand(commands));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SendToBack()
    {
        var commands = Selection.Reverse().Select(a => (IEditCommand)new ReorderAnnotationCommand(a, 0)).ToArray();
        Document.Execute(new CompositeCommand(commands));
    }

    [RelayCommand]
    private void SelectAll()
    {
        ActiveTool = EditorTool.Select;
        Selection = Annotations.ToArray();
    }

    [RelayCommand]
    private void Deselect() => Selection = [];

    [RelayCommand]
    private void Nudge(string direction)
    {
        var (dx, dy) = direction switch
        {
            "left" => (-1, 0),
            "right" => (1, 0),
            "up" => (0, -1),
            _ => (0, 1),
        };
        if (Selection.Count == 0)
        {
            return;
        }

        var pairs = Selection.Select(a => (Before: a, After: a.Translate(dx, dy))).ToArray();
        Document.Execute(new CompositeCommand(pairs.Select(p => (IEditCommand)new ReplaceAnnotationCommand(p.Before, p.After)).ToArray()));
        Selection = pairs.Select(p => p.After).ToArray();
    }

    [RelayCommand]
    private void ZoomIn() => ZoomBy(1.25);

    [RelayCommand]
    private void ZoomOut() => ZoomBy(0.8);

    [RelayCommand]
    private void ZoomToActual()
    {
        FitToWindow = false;
        Zoom = 1;
    }

    [RelayCommand]
    private void ZoomToFit() => FitToWindow = true;

    [RelayCommand]
    private void Copy()
    {
        Export.ClipboardImageService.SetImage(DocumentRenderer.Render(Document, CanvasBackground));
        Close();
    }

    [RelayCommand]
    private void Save()
    {
        _save(DocumentRenderer.Render(Document, CanvasBackground));
        Close();
    }

    [RelayCommand]
    private void SaveAs()
    {
        if (_saveAs(DocumentRenderer.Render(Document, CanvasBackground)) is not null)
        {
            Close();
        }
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
