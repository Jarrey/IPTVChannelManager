using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace IPTVChannelManager.Common
{
    //
    // Summary:
    //     Represents each node of nested properties expression and takes care of subscribing/unsubscribing
    //     INotifyPropertyChanged.PropertyChanged listeners on it.
    internal class PropertyObserverNode
    {
        private readonly Action _action;

        private INotifyPropertyChanged _inpcObject;

        public PropertyInfo PropertyInfo { get; }

        public PropertyObserverNode Next { get; set; }

        public PropertyObserverNode(PropertyInfo propertyInfo, Action action)
        {
            PropertyObserverNode propertyObserverNode = this;
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException("propertyInfo");
            _action = delegate
            {
                action?.Invoke();
                if (propertyObserverNode.Next != null)
                {
                    propertyObserverNode.Next.UnsubscribeListener();
                    propertyObserverNode.GenerateNextNode();
                }
            };
        }

        public void SubscribeListenerFor(INotifyPropertyChanged inpcObject)
        {
            _inpcObject = inpcObject;
            _inpcObject.PropertyChanged += OnPropertyChanged;
            if (Next != null)
            {
                GenerateNextNode();
            }
        }

        private void GenerateNextNode()
        {
            object value = PropertyInfo.GetValue(_inpcObject);
            if (value != null)
            {
                INotifyPropertyChanged notifyPropertyChanged = value as INotifyPropertyChanged;
                if (notifyPropertyChanged == null)
                {
                    throw new InvalidOperationException("Trying to subscribe PropertyChanged listener in object that owns '" + Next.PropertyInfo.Name + "' property, but the object does not implements INotifyPropertyChanged.");
                }

                Next.SubscribeListenerFor(notifyPropertyChanged);
            }
        }

        private void UnsubscribeListener()
        {
            if (_inpcObject != null)
            {
                _inpcObject.PropertyChanged -= OnPropertyChanged;
            }

            Next?.UnsubscribeListener();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == PropertyInfo.Name || string.IsNullOrEmpty(e?.PropertyName))
            {
                _action?.Invoke();
            }
        }
    }

    //
    // Summary:
    //     Provide a way to observe property changes of INotifyPropertyChanged objects and
    //     invokes a custom action when the PropertyChanged event is fired.
    internal class PropertyObserver
    {
        private readonly Action _action;

        private PropertyObserver(Expression propertyExpression, Action action)
        {
            _action = action;
            SubscribeListeners(propertyExpression);
        }

        private void SubscribeListeners(Expression propertyExpression)
        {
            Stack<PropertyInfo> stack = new Stack<PropertyInfo>();
            while (true)
            {
                MemberExpression memberExpression = propertyExpression as MemberExpression;
                if (memberExpression == null)
                {
                    break;
                }

                propertyExpression = memberExpression.Expression;
                stack.Push(memberExpression.Member as PropertyInfo);
            }

            ConstantExpression constantExpression = propertyExpression as ConstantExpression;
            if (constantExpression == null)
            {
                throw new NotSupportedException("Operation not supported for the given expression type. Only MemberExpression and ConstantExpression are currently supported.");
            }

            PropertyObserverNode propertyObserverNode = new PropertyObserverNode(stack.Pop(), _action);
            PropertyObserverNode propertyObserverNode2 = propertyObserverNode;
            using (Stack<PropertyInfo>.Enumerator enumerator = stack.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    PropertyObserverNode propertyObserverNode4 = propertyObserverNode2.Next = new PropertyObserverNode(enumerator.Current, _action);
                    propertyObserverNode2 = propertyObserverNode4;
                }
            }

            INotifyPropertyChanged notifyPropertyChanged = constantExpression.Value as INotifyPropertyChanged;
            if (notifyPropertyChanged == null)
            {
                throw new InvalidOperationException("Trying to subscribe PropertyChanged listener in object that owns '" + propertyObserverNode.PropertyInfo.Name + "' property, but the object does not implements INotifyPropertyChanged.");
            }

            propertyObserverNode.SubscribeListenerFor(notifyPropertyChanged);
        }

        //
        // Summary:
        //     Observes a property that implements INotifyPropertyChanged, and automatically
        //     calls a custom action on property changed notifications. The given expression
        //     must be in this form: "() => Prop.NestedProp.PropToObserve".
        //
        // Parameters:
        //   propertyExpression:
        //     Expression representing property to be observed. Ex.: "() => Prop.NestedProp.PropToObserve".
        //
        //   action:
        //     Action to be invoked when PropertyChanged event occurs.
        internal static PropertyObserver Observes<T>(Expression<Func<T>> propertyExpression, Action action)
        {
            return new PropertyObserver(propertyExpression.Body, action);
        }
    }
}
