// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using SharedToolbox;
using System.ComponentModel;
using System.Windows;

namespace SamNet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private EdwardsMessageBus eventBus;
        public MainWindow()
        {
            InitializeComponent();

            eventBus = (EdwardsMessageBus)App.Current.Resources["EventBus"];
            string assemblyVersion = typeof(MainWindow).Assembly.GetName().Version?.ToString()!;
            this.Title = "SamNet (" + assemblyVersion + ")";

        }

        #region ScaleValue Dependency Property

        // Main Program scaling based on concepts explained at
        //http://stackoverflow.com/questions/3193339/tips-on-developing-resolution-independent-application/5000120#5000120
        // Thanks to JacobJ etal

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            MainWindow? mainWindow = o as MainWindow;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            else
                return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow? mainWindow = o as MainWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {

        }

        public double ScaleValue
        {
            get
            {
                return (double)GetValue(ScaleValueProperty);
            }
            set
            {
                SetValue(ScaleValueProperty, value);
            }
        }
        #endregion

        public void Close_Interface(object sender, CancelEventArgs e)
        {
            if (SamModel.CurrentPage == 4) return;
            eventBus.Publish(new EventMessage(1, 0, EventEnum.Close));
            e.Cancel = true;
        }

        public void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            /*
            _ao.WS.Left = (int)this.Left;
            _ao.WS.Top = (int)this.Top;
            _ao.WS.Width = (int)this.Width;
            _ao.WS.Height = (int)this.Height;
            */
        }

        public void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            /*
            _ao.WS.Left = (int)this.Left;
            _ao.WS.Top = (int)this.Top;
            _ao.WS.Width = (int)this.Width;
            _ao.WS.Height = (int)this.Height;
            */
        }

        public void MainWindow_StateChanged(object sender, EventArgs e)
        {
            ProcessMainWindowStateChanged();
        }

        public void ProcessMainWindowStateChanged()
        {

        }

        private void MainGrid_SizeChanged(object sender, EventArgs e)
        {
            CalculateScale();
        }

        private void CalculateScale()
        {
            double yScale = ActualHeight / 1000f;
            double xScale = ActualWidth / 1800f;
            double value = Math.Min(xScale, yScale);
            ScaleValue = (double)OnCoerceScaleValue(WpfCore8BaseMainWindow, value);
        }
    }


}