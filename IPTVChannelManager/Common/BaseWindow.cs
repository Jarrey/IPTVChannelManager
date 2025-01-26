using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace IPTVChannelManager.Common
{
    public abstract class BaseWindow : Window
    {
        public static string ActivatedWindowName { get; private set; }
        static BaseWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BaseWindow), new FrameworkPropertyMetadata(typeof(BaseWindow)));
        }

        public BaseWindow()
        {
            InitializeWindowCommands();
            Name = GetType().Name;
            Closed += BaseWindowClosed;
            Activated += WindowActivated;
        }

        #region Dependency Properties
        public static readonly DependencyProperty IconTooltipProperty =
            DependencyProperty.Register(nameof(IconTooltip), typeof(string), typeof(BaseWindow), new PropertyMetadata(null));
        public string IconTooltip
        {
            get => (string)GetValue(IconTooltipProperty);
            set => SetValue(IconTooltipProperty, value);
        }

        public bool HighlightOnActivated
        {
            get => (bool)GetValue(HighlightOnActivatedProperty);
            set => SetValue(HighlightOnActivatedProperty, value);
        }
        public static readonly DependencyProperty HighlightOnActivatedProperty =
            DependencyProperty.Register(nameof(HighlightOnActivated), typeof(bool), typeof(BaseWindow), new PropertyMetadata(false));

        public UIElement CustomButtons
        {
            get => (UIElement)GetValue(CustomButtonsProperty);
            set => SetValue(CustomButtonsProperty, value);
        }
        public static readonly DependencyProperty CustomButtonsProperty =
            DependencyProperty.Register(nameof(CustomButtons), typeof(UIElement), typeof(BaseWindow), new PropertyMetadata(null));

        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }
        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(nameof(ShowIcon), typeof(bool), typeof(BaseWindow), new PropertyMetadata(true));
        #endregion Dependency Properties

        private void InitializeWindowCommands()
        {
            void canResize(object o, CanExecuteRoutedEventArgs e) => e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;

            _ = CommandBindings.Add(
                new CommandBinding(
                    SystemCommands.CloseWindowCommand,
                    (o, e) => SystemCommands.CloseWindow(this)));
            _ = CommandBindings.Add(
            new CommandBinding(
                    SystemCommands.MaximizeWindowCommand,
                    (o, e) => { SystemCommands.MaximizeWindow(this); e.Handled = true; },
                    canResize));
            _ = CommandBindings.Add(
                new CommandBinding(
                    SystemCommands.MinimizeWindowCommand,
                    (o, e) => { SystemCommands.MinimizeWindow(this); e.Handled = true; },
                    (o, e) => e.CanExecute = ResizeMode != ResizeMode.NoResize));
            _ = CommandBindings.Add(
            new CommandBinding(
                    SystemCommands.RestoreWindowCommand,
                    (o, e) => { SystemCommands.RestoreWindow(this); e.Handled = true; },
                    canResize));
            _ = CommandBindings.Add(
                new CommandBinding(
                    SystemCommands.ShowSystemMenuCommand,
                    (o, e) =>
                    {
                        if (e.OriginalSource is not FrameworkElement element)
                        {
                            return;
                        }

                        Point point = WindowState == WindowState.Maximized ? new Point(0, 30) : new Point(Left + BorderThickness.Left, 30 + Top + BorderThickness.Top);
                        point = element.TransformToAncestor(this).Transform(point);
                        SystemCommands.ShowSystemMenu(this, point);
                        e.Handled = true;
                    },
                    canResize));
        }

        private void WindowActivated(object sender, EventArgs e)
        {
            ActivatedWindowName = Name;
        }

        private void BaseWindowClosed(object sender, EventArgs e)
        {
            Activated -= WindowActivated;
            Closed -= BaseWindowClosed;
            if (DataContext is IDisposable obj)
            {
                DataContext = null;
                obj.Dispose();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new CustomWindowAutomationPeer(this);

        private class CustomWindowAutomationPeer : FrameworkElementAutomationPeer
        {
            public CustomWindowAutomationPeer(FrameworkElement owner) : base(owner) { }

            protected override string GetAutomationIdCore() => string.Empty;

            protected override string GetNameCore() => string.Empty;

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

            protected override List<AutomationPeer> GetChildrenCore() => new();
        }
    }
}
