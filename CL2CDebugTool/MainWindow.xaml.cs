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
        }

        private void OnLoggingHandle(object sender,string log)
        {
            if(txtLog.LineCount>1000)
            {
                txtLog.Clear();
            }
            txtLog.AppendText(log);
            txtLog.AppendText(Environment.NewLine);
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
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _controller.Dispose();
            Logger.LogTextWriter.GetInstance().OnLogging -= OnLoggingHandle;
        }

        private void btnRetZero_Click(object sender, RoutedEventArgs e)
        {
            RetrunZeroDirection direction= radBack.IsChecked==true? RetrunZeroDirection.Backward : RetrunZeroDirection.Forward;
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
    }
}