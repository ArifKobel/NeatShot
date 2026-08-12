using NeatShot.Core.Capture;

namespace NeatShot.Core.Annotations;

public sealed class AnnotationDocument
{
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
