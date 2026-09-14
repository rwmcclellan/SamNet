using System;
using System.Diagnostics;
using System.Windows.Input;

namespace SharedToolbox
{
    // A command to relay its functionality to other objects by invoking delegates
    // The default return value for the CanExecute method is 'true'

    public class RelayCommand : ICommand
    {
        #region Fields

        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;

        #endregion // Fields

        #region Constructors

        // Creates a new command that can always execute.

        public RelayCommand(Action<object> execute) : this(execute, null!)
        {
        }

        // Creates a new command.
        // execute - The execution logic.
        // canExecute - The execution status logic.

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion // Constructors

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object? parameters)
        {
            return _canExecute == null ? true : _canExecute(parameters!);
        }

#pragma warning disable 8612
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object? parameters)
        {
            _execute(parameters!);
        }

        #endregion // ICommand Members
    }
}