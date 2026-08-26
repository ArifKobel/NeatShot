using NeatShot.Core.Capture;

namespace NeatShot.Core.Annotations;

public sealed class AnnotationDocument
{
    private const double CanvasPadding = 8;

    private readonly List<Annotation> _annotations = [];
    private readonly EditHistory _history = new();

    public AnnotationDocument(CapturedImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = image;
        _history.Changed += (_, _) => HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;

    public event EventHandler? HistoryChanged;

    public CapturedImage Image { get; }

    public IReadOnlyList<Annotation> Annotations => _annotations;

    public ImageRect ImageBounds => new(0, 0, Image.Width, Image.Height);

    public ImageRect Canvas => CanvasAround(_annotations);

    public ImageRect CanvasAround(IEnumerable<Annotation> annotations)
    {
        ArgumentNullException.ThrowIfNull(annotations);

        var canvas = ImageBounds;
        foreach (var annotation in annotations)
        {
            var reach = annotation.Bounds.Inflate(CanvasPadding);
            if (reach.Left < canvas.Left || reach.Top < canvas.Top || reach.Right > canvas.Right || reach.Bottom > canvas.Bottom)
            {
                canvas = canvas.Union(reach);
            }
        }

        return ImageRect.FromPoints(
            new ImagePoint(Math.Floor(canvas.Left), Math.Floor(canvas.Top)),
            new ImagePoint(Math.Ceiling(canvas.Right), Math.Ceiling(canvas.Bottom)));
    }

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public int NextCounterNumber => _annotations.OfType<CounterAnnotation>().Select(c => c.Number).DefaultIfEmpty(0).Max() + 1;

    public void Execute(IEditCommand command) => _history.Execute(command, this);

    public bool Undo() => _history.Undo(this);

    public bool Redo() => _history.Redo(this);

    public Annotation? HitTest(ImagePoint point)
    {
        for (var i = _annotations.Count - 1; i >= 0; i--)
        {
            if (_annotations[i].HitTest(point))
            {
                return _annotations[i];
            }
        }

        return null;
    }

    public Annotation? Find(Guid id) => _annotations.Find(a => a.Id == id);

    internal void Add(Annotation annotation) => Insert(_annotations.Count, annotation);

    internal void Insert(int index, Annotation annotation)
    {
        _annotations.Insert(Math.Clamp(index, 0, _annotations.Count), annotation);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal int Remove(Guid id)
    {
        var index = _annotations.FindIndex(a => a.Id == id);
        if (index < 0)
        {
            throw new InvalidOperationException("Annotation is not part of the document.");
        }

        _annotations.RemoveAt(index);
        Changed?.Invoke(this, EventArgs.Empty);
        return index;
    }

    internal int Move(Guid id, int targetIndex)
    {
        var index = _annotations.FindIndex(a => a.Id == id);
        if (index < 0)
        {
            throw new InvalidOperationException("Annotation is not part of the document.");
        }

        var annotation = _annotations[index];
        _annotations.RemoveAt(index);
        _annotations.Insert(Math.Clamp(targetIndex, 0, _annotations.Count), annotation);
        Changed?.Invoke(this, EventArgs.Empty);
        return index;
    }

    internal void Replace(Annotation annotation)
    {
        var index = _annotations.FindIndex(a => a.Id == annotation.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("Annotation is not part of the document.");
        }

        _annotations[index] = annotation;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
