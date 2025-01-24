using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Windows.Input;

namespace IPTVChannelManager
{    //
    // Summary:
    //     Interface that defines if the object instance is active and notifies when the
    //     activity changes.
    public interface IActiveAware
    {
        //
        // Summary:
        //     Gets or sets a value indicating whether the object is active.
        //
        // Value:
        //     true if the object is active; otherwise false.
        bool IsActive { get; set; }

        //
        // Summary:
        //     Notifies that the value for Prism.IActiveAware.IsActive property has changed.
        event EventHandler IsActiveChanged;
    }

    //
    // Summary:
    //     An System.Windows.Input.ICommand whose delegates can be attached for Prism.Commands.DelegateCommandBase.Execute(System.Object)
    //     and Prism.Commands.DelegateCommandBase.CanExecute(System.Object).
    public abstract class DelegateCommandBase : ICommand, IActiveAware
    {
        private bool _isActive;

        private SynchronizationContext _synchronizationContext;

        private readonly HashSet<string> _observedPropertiesExpressions = new HashSet<string>();

        //
        // Summary:
        //     Gets or sets a value indicating whether the object is active.
        //
        // Value:
        //     true if the object is active; otherwise false.
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnIsActiveChanged();
                }
            }
        }

        //
        // Summary:
        //     Occurs when changes occur that affect whether or not the command should execute.
        public virtual event EventHandler CanExecuteChanged;

        //
        // Summary:
        //     Fired if the Prism.Commands.DelegateCommandBase.IsActive property changes.
        public virtual event EventHandler IsActiveChanged;

        //
        // Summary:
        //     Creates a new instance of a Prism.Commands.DelegateCommandBase, specifying both
        //     the execute action and the can execute function.
        protected DelegateCommandBase()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        //
        // Summary:
        //     Raises System.Windows.Input.ICommand.CanExecuteChanged so every command invoker
        //     can requery System.Windows.Input.ICommand.CanExecute(System.Object).
        protected virtual void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler == null)
            {
                return;
            }

            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                _synchronizationContext.Post(delegate
                {
                    handler(this, EventArgs.Empty);
                }, null);
            }
            else
            {
                handler(this, EventArgs.Empty);
            }
        }

        //
        // Summary:
        //     Raises Prism.Commands.DelegateCommandBase.CanExecuteChanged so every command
        //     invoker can requery to check if the command can execute.
        //
        // Remarks:
        //     Note that this will trigger the execution of Prism.Commands.DelegateCommandBase.CanExecuteChanged
        //     once for each invoker.
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        void ICommand.Execute(object parameter)
        {
            Execute(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter);
        }

        //
        // Summary:
        //     Handle the internal invocation of System.Windows.Input.ICommand.Execute(System.Object)
        //
        // Parameters:
        //   parameter:
        //     Command Parameter
        protected abstract void Execute(object parameter);

        //
        // Summary:
        //     Handle the internal invocation of System.Windows.Input.ICommand.CanExecute(System.Object)
        //
        // Parameters:
        //   parameter:
        //
        // Returns:
        //     true if the Command Can Execute, otherwise false
        protected abstract bool CanExecute(object parameter);

        //
        // Summary:
        //     Observes a property that implements INotifyPropertyChanged, and automatically
        //     calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
        //
        // Parameters:
        //   propertyExpression:
        //     The property expression. Example: ObservesProperty(() => PropertyName).
        //
        // Type parameters:
        //   T:
        //     The object type containing the property specified in the expression.
        protected internal void ObservesPropertyInternal<T>(Expression<Func<T>> propertyExpression)
        {
            if (_observedPropertiesExpressions.Contains(propertyExpression.ToString()))
            {
                throw new ArgumentException(propertyExpression.ToString() + " is already being observed.", "propertyExpression");
            }

            _observedPropertiesExpressions.Add(propertyExpression.ToString());
            PropertyObserver.Observes(propertyExpression, RaiseCanExecuteChanged);
        }

        //
        // Summary:
        //     This raises the Prism.Commands.DelegateCommandBase.IsActiveChanged event.
        protected virtual void OnIsActiveChanged()
        {
            this.IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
