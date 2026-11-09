using System;
using System.Windows.Input;

namespace DesktopClock;

/// <summary>
/// A command that runs an action, for binding menu items and buttons to methods.
/// </summary>
public sealed class RelayCommand(Action execute) : ICommand
{
    // The commands can always run, so there's never a change to report.
    public event EventHandler CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object parameter) => true;

    public void Execute(object parameter) => execute();
}
