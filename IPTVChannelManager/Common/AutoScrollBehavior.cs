using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace IPTVChannelManager.Common
{
    /// <summary>
    /// Attached behavior that automatically scrolls an <see cref="ItemsControl"/>
    /// to the last item whenever its <see cref="INotifyCollectionChanged"/> source changes.
    /// Attach to any <see cref="ListBox"/> or <see cref="ListView"/> in XAML:
    /// <code>
    ///   &lt;i:Interaction.Behaviors&gt;
    ///     &lt;common:AutoScrollBehavior /&gt;
    ///   &lt;/i:Interaction.Behaviors&gt;
    /// </code>
    /// </summary>
    public class AutoScrollBehavior : Behavior<ListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            UnsubscribeCollection();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SubscribeCollection();
            // Re-subscribe when ItemsSource changes
            var descriptor = System.ComponentModel.DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            descriptor?.AddValueChanged(AssociatedObject, OnItemsSourceChanged);
        }

        private void OnItemsSourceChanged(object? sender, System.EventArgs e)
        {
            UnsubscribeCollection();
            SubscribeCollection();
        }

        private INotifyCollectionChanged? _subscribedCollection;

        private void SubscribeCollection()
        {
            if (AssociatedObject.ItemsSource is INotifyCollectionChanged collection)
            {
                _subscribedCollection = collection;
                _subscribedCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void UnsubscribeCollection()
        {
            if (_subscribedCollection != null)
            {
                _subscribedCollection.CollectionChanged -= OnCollectionChanged;
                _subscribedCollection = null;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                int count = AssociatedObject.Items.Count;
                if (count > 0)
                    AssociatedObject.ScrollIntoView(AssociatedObject.Items[count - 1]);
            });
        }
    }
}
