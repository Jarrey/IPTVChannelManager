using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;

namespace IPTVChannelManager.Common
{
    //
    // Summary:
    //     An System.Windows.Input.ICommand whose delegates do not take any parameters for
    //     Prism.Commands.DelegateCommand.Execute and Prism.Commands.DelegateCommand.CanExecute.
    public class DelegateCommand : DelegateCommandBase
    {
        private Action _executeMethod;

        private Func<bool> _canExecuteMethod;

        //
        // Summary:
        //     Creates a new instance of Prism.Commands.DelegateCommand with the System.Action
        //     to invoke on execution.
        //
        // Parameters:
        //   executeMethod:
        //     The System.Action to invoke when System.Windows.Input.ICommand.Execute(System.Object)
        //     is called.
        public DelegateCommand(Action executeMethod)
            : this(executeMethod, () => true)
        {
        }

        //
        // Summary:
        //     Creates a new instance of Prism.Commands.DelegateCommand with the System.Action
        //     to invoke on execution and a Func to query for determining if the command can
        //     execute.
        //
        // Parameters:
        //   executeMethod:
        //     The System.Action to invoke when System.Windows.Input.ICommand.Execute(System.Object)
        //     is called.
        //
        //   canExecuteMethod:
        //     The System.Func`1 to invoke when System.Windows.Input.ICommand.CanExecute(System.Object)
        //     is called
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            if (executeMethod == null || canExecuteMethod == null)
            {
                throw new ArgumentNullException("executeMethod", "DelegateCommandDelegatesCannotBeNull");
            }

            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        //
        // Summary:
        //     Executes the command.
        public void Execute()
        {
            _executeMethod();
        }

        //
        // Summary:
        //     Determines if the command can be executed.
        //
        // Returns:
        //     Returns true if the command can execute,otherwise returns false.
        public bool CanExecute()
        {
            return _canExecuteMethod();
        }

        //
        // Summary:
        //     Handle the internal invocation of System.Windows.Input.ICommand.Execute(System.Object)
        //
        // Parameters:
        //   parameter:
        //     Command Parameter
        protected override void Execute(object parameter)
        {
            Execute();
        }

        //
        // Summary:
        //     Handle the internal invocation of System.Windows.Input.ICommand.CanExecute(System.Object)
        //
        // Parameters:
        //   parameter:
        //
        // Returns:
        //     true if the Command Can Execute, otherwise false
        protected override bool CanExecute(object parameter)
        {
            return CanExecute();
        }

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
        //
        // Returns:
        //     The current instance of DelegateCommand
        public DelegateCommand ObservesProperty<T>(Expression<Func<T>> propertyExpression)
        {
            ObservesPropertyInternal(propertyExpression);
            return this;
        }

        //
        // Summary:
        //     Observes a property that is used to determine if this command can execute, and
        //     if it implements INotifyPropertyChanged it will automatically call DelegateCommandBase.RaiseCanExecuteChanged
        //     on property changed notifications.
        //
        // Parameters:
        //   canExecuteExpression:
        //     The property expression. Example: ObservesCanExecute(() => PropertyName).
        //
        // Returns:
        //     The current instance of DelegateCommand
        public DelegateCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
        {
            _canExecuteMethod = canExecuteExpression.Compile();
            ObservesPropertyInternal(canExecuteExpression);
            return this;
        }
    }


    //
    // Summary:
    //     An System.Windows.Input.ICommand whose delegates can be attached for Prism.Commands.DelegateCommand`1.Execute(`0)
    //     and Prism.Commands.DelegateCommand`1.CanExecute(`0).
    //
    // Type parameters:
    //   T:
    //     Parameter type.
    //
    // Remarks:
    //     The constructor deliberately prevents the use of value types. Because ICommand
    //     takes an object, having a value type for T would cause unexpected behavior when
    //     CanExecute(null) is called during XAML initialization for command bindings. Using
    //     default(T) was considered and rejected as a solution because the implementor
    //     would not be able to distinguish between a valid and defaulted values.
    //     Instead, callers should support a value type by using a nullable value type and
    //     checking the HasValue property before using the Value property.
    //     public MyClass()
    //     {
    //         this.submitCommand = new DelegateCommand<int?>(this.Submit, this.CanSubmit);
    //     }
    //     private bool CanSubmit(int? customerId)
    //     {
    //         return (customerId.HasValue && customers.Contains(customerId.Value));
    //     }
    public class DelegateCommand<T> : DelegateCommandBase
    {
        private readonly Action<T> _executeMethod;

        private Func<T, bool> _canExecuteMethod;

        //
        // Summary:
        //     Initializes a new instance of Prism.Commands.DelegateCommand`1.
        //
        // Parameters:
        //   executeMethod:
        //     Delegate to execute when Execute is called on the command. This can be null to
        //     just hook up a CanExecute delegate.
        //
        // Remarks:
        //     Prism.Commands.DelegateCommand`1.CanExecute(`0) will always return true.
        public DelegateCommand(Action<T> executeMethod)
            : this(executeMethod, (o) => true)
        {
        }

        //
        // Summary:
        //     Initializes a new instance of Prism.Commands.DelegateCommand`1.
        //
        // Parameters:
        //   executeMethod:
        //     Delegate to execute when Execute is called on the command. This can be null to
        //     just hook up a CanExecute delegate.
        //
        //   canExecuteMethod:
        //     Delegate to execute when CanExecute is called on the command. This can be null.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     When both executeMethod and canExecuteMethod are null.
        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            if (executeMethod == null || canExecuteMethod == null)
            {
                throw new ArgumentNullException("executeMethod", "DelegateCommandDelegatesCannotBeNull");
            }

            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            if (typeInfo.IsValueType && (!typeInfo.IsGenericType || !typeof(Nullable<>).GetTypeInfo().IsAssignableFrom(typeInfo.GetGenericTypeDefinition().GetTypeInfo())))
            {
                throw new InvalidCastException("DelegateCommandInvalidGenericPayloadType");
            }

            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        //
        // Summary:
        //     Executes the command and invokes the System.Action`1 provided during construction.
        //
        // Parameters:
        //   parameter:
        //     Data used by the command.
        public void Execute(T parameter)
        {
            _executeMethod(parameter);
        }

        //
        // Summary:
        //     Determines if the command can execute by invoked the System.Func`2 provided during
        //     construction.
        //
        // Parameters:
        //   parameter:
        //     Data used by the command to determine if it can execute.
        //
        // Returns:
        //     true if this command can be executed; otherwise, false.
        public bool CanExecute(T parameter)
        {
            return _canExecuteMethod(parameter);
        }

        //
        // Summary:
        //     Handle the internal invocation of System.Windows.Input.ICommand.Execute(System.Object)
        //
        // Parameters:
        //   parameter:
        //     Command Parameter
        protected override void Execute(object parameter)
        {
            Execute((T)parameter);
        }

        //
        // Summary:
        //     Handle the internal invocation of System.Windows.Input.ICommand.CanExecute(System.Object)
        //
        // Parameters:
        //   parameter:
        //
        // Returns:
        //     true if the Command Can Execute, otherwise false
        protected override bool CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }

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
        //   TType:
        //     The type of the return value of the method that this delegate encapsulates
        //
        // Returns:
        //     The current instance of DelegateCommand
        public DelegateCommand<T> ObservesProperty<TType>(Expression<Func<TType>> propertyExpression)
        {
            ObservesPropertyInternal(propertyExpression);
            return this;
        }

        //
        // Summary:
        //     Observes a property that is used to determine if this command can execute, and
        //     if it implements INotifyPropertyChanged it will automatically call DelegateCommandBase.RaiseCanExecuteChanged
        //     on property changed notifications.
        //
        // Parameters:
        //   canExecuteExpression:
        //     The property expression. Example: ObservesCanExecute(() => PropertyName).
        //
        // Returns:
        //     The current instance of DelegateCommand
        public DelegateCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
        {
            Expression<Func<T, bool>> expression = Expression.Lambda<Func<T, bool>>(canExecuteExpression.Body, new ParameterExpression[1] { Expression.Parameter(typeof(T), "o") });
            _canExecuteMethod = expression.Compile();
            ObservesPropertyInternal(canExecuteExpression);
            return this;
        }
    }
}
