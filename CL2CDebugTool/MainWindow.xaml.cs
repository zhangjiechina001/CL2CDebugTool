using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CL2CDebugTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CL2CController _controller;
        private readonly DispatcherTimer _timer;
        public MainWindow()
        {
            InitializeComponent();
            _controller= new CL2CController();
            DataContext = _controller;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Console.WriteLine($"MainWindow {Thread.CurrentThread.ManagedThreadId}");
            Logger.LogTextWriter.GetInstance().OnLogging += OnLoggingHandle;

            cmbReturnMode.ItemsSource = new List<ReturnMode>() { ReturnMode.Limit, ReturnMode.Zero };
            panelControl.IsEnabled = false;
        }

        private void OnLoggingHandle(object sender,string log)
        {
            if(txtLog.LineCount>1000)
            {
                txtLog.Clear();
            }
            txtLog.AppendText(log);
            txtLog.AppendText(Environment.NewLine);
            txtLog.ScrollToEnd();
            //txtLog.
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine($"Timer_Tick {Thread.CurrentThread.ManagedThreadId}");
            if (chbAutoUpdate.IsChecked == true&& _controller.IsConnected)
            {
                _controller.UpdateState();
                dgvStateItems.InvalidateVisual();
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.ConnectToHost(txtAddr.Text);
            (sender as Control).IsEnabled = false;
            panelControl.IsEnabled = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _controller.Dispose();
            Logger.LogTextWriter.GetInstance().OnLogging -= OnLoggingHandle;
        }

        private void btnRetZero_Click(object sender, RoutedEventArgs e)
        {
            AxisDirection direction= radBack.IsChecked==true? AxisDirection.Backward : AxisDirection.Forward;
            ReturnMode mode = (ReturnMode)cmbReturnMode.SelectedItem;
            _controller.ReturnToZero(direction, mode);
        }

        private void btnRetCuruentZero_Click(object sender, RoutedEventArgs e)
        {
            _controller.SetCurrentToZero();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            _controller.Stop();
        }

        private void btnSetVel_Click(object sender, RoutedEventArgs e)
        {
            double val = double.Parse(txtVel.Text);
            _controller.SetVel(val);
        }

        private void btnReverseMove_Click(object sender, RoutedEventArgs e)
        {
            double val = double.Parse(txtAbsVal.Text);
            _controller.RelativeMove(-val);
        }

        private void btnForwardMove_Click(object sender, RoutedEventArgs e)
        {
            double val = double.Parse(txtAbsVal.Text);
            _controller.RelativeMove(val);
        }

        private void btnSetLimit_Click(object sender, RoutedEventArgs e)
        {
            if(chbBack.IsChecked==true)
            {
                _controller.SetLimit(AxisDirection.Backward, true);
            }
            if (chbForward.IsChecked == true)
            {
                _controller.SetLimit(AxisDirection.Forward, true);
            }
        }

        private void btnGetLimit_Click(object sender, RoutedEventArgs e)
        {
            bool enable1=false;
            _controller.GetLimit(AxisDirection.Backward,ref enable1);
            chbBack.IsChecked = enable1;

            bool enable2 = false;
            _controller.GetLimit(AxisDirection.Forward, ref enable2);
            chbForward.IsChecked = enable2;
        }

        private void btnsSaveParam_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}