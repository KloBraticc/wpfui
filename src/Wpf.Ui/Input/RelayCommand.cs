using System;
using System.Windows.Input;

namespace Wpf.Ui.Input;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;

    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action<object?> execute)
        : this(execute, null)
    {
    }

    public RelayCommand(Action<object?> execute, Func<bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
