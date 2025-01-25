using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace IPTVChannelManager.Common
{
    public class ItemsControlFilter
    {
        public static readonly DependencyProperty ByProperty = DependencyProperty.RegisterAttached(
           "By",
           typeof(Predicate<object>),
           typeof(ItemsControlFilter),
           new PropertyMetadata(default(Predicate<object>), OnByChanged));

        public static void SetBy(ItemsControl element, Predicate<object> value)
        {
            element.SetValue(ByProperty, value);
        }

        public static Predicate<object> GetBy(ItemsControl element)
        {
            return (Predicate<object>)element.GetValue(ByProperty);
        }

        private static void OnByChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControl itemsControl && itemsControl.Items?.CanFilter == true)
            {
                itemsControl.Items.Filter = (Predicate<object>)e.NewValue;
            }
        }
    }
}
