namespace NeatShot.Core.Annotations;

public interface IEditCommand
{
    void Execute(AnnotationDocument document);

    void Undo(AnnotationDocument document);
}

public sealed class EditHistory
{
    private readonly Stack<IEditCommand> _undo = new();
    private readonly Stack<IEditCommand> _redo = new();

    public event EventHandler? Changed;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Execute(IEditCommand command, AnnotationDocument document)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(document);

        command.Execute(document);
        _undo.Push(command);
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Undo(AnnotationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!_undo.TryPop(out var command))
        {
            return false;
        }

        command.Undo(document);
        _redo.Push(command);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Redo(AnnotationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!_redo.TryPop(out var command))
        {
            return false;
        }

        command.Execute(document);
        _undo.Push(command);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}

public sealed class AddAnnotationCommand : IEditCommand
{
    private readonly Annotation _annotation;

    public AddAnnotationCommand(Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        _annotation = annotation;
    }

    public void Execute(AnnotationDocument document) => document.Add(_annotation);

    public void Undo(AnnotationDocument document) => document.Remove(_annotation.Id);
}

public sealed class RemoveAnnotationCommand : IEditCommand
{
    private readonly Annotation _annotation;
    private int _index;

    public RemoveAnnotationCommand(Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        _annotation = annotation;
    }

    public void Execute(AnnotationDocument document) => _index = document.Remove(_annotation.Id);

    public void Undo(AnnotationDocument document) => document.Insert(_index, _annotation);
}

public sealed class ReplaceAnnotationCommand : IEditCommand
{
    private readonly Annotation _before;
    private readonly Annotation _after;

    public ReplaceAnnotationCommand(Annotation before, Annotation after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (before.Id != after.Id)
        {
            throw new ArgumentException("Replacement must keep the annotation id.", nameof(after));
        }

        _before = before;
        _after = after;
    }

    public void Execute(AnnotationDocument document) => document.Replace(_after);

    public void Undo(AnnotationDocument document) => document.Replace(_before);
}

public sealed class ReorderAnnotationCommand : IEditCommand
{
    private readonly Guid _id;
    private readonly int _targetIndex;
    private int _sourceIndex;

    public ReorderAnnotationCommand(Annotation annotation, int targetIndex)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        _id = annotation.Id;
        _targetIndex = targetIndex;
    }

    public void Execute(AnnotationDocument document) => _sourceIndex = document.Move(_id, _targetIndex);

    public void Undo(AnnotationDocument document) => document.Move(_id, _sourceIndex);
}

public sealed class CompositeCommand : IEditCommand
{
    private readonly IReadOnlyList<IEditCommand> _commands;

    public CompositeCommand(IReadOnlyList<IEditCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        _commands = commands;
    }

    public void Execute(AnnotationDocument document)
    {
        foreach (var command in _commands)
        {
            command.Execute(document);
        }
    }

    public void Undo(AnnotationDocument document)
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo(document);
        }
    }
}
