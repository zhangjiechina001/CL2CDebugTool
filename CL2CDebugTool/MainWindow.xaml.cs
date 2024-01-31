using System.ComponentModel;
using System.Configuration;
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
using static System.Net.Mime.MediaTypeNames;

namespace CL2CDebugTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //1.回零测试结果
        //2.限位卡死
        //3.丢步问题和到轨到原点有关系
        //4.使用tcp设置导轨
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
            InitIP();
        }

        private void InitIP()
        {
            if(ConfigurationManager.AppSettings.AllKeys.Contains("IP"))
            {
                string ip = ConfigurationManager.AppSettings["IP"];
                txtAddr.Text = ip;
            }
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
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (chbAutoUpdate.IsChecked == true && _controller.IsConnected)
                {
                    _controller.UpdateState();
                    dgvStateItems.InvalidateVisual();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据更新失败{ex.Message}");
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.ConnectToHost(txtAddr.Text);
            (sender as Control).IsEnabled = false;
            panelControl.IsEnabled = true;
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings["IP"].Value = txtAddr.Text;
            cfa.Save();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _controller.Dispose();
            Logger.LogTextWriter.GetInstance().OnLogging -= OnLoggingHandle;
        }

        private void btnRetZero_Click(object sender, RoutedEventArgs e)
        {
            if(cmbReturnMode.SelectedIndex>=0)
            {
                AxisDirection direction = radBack.IsChecked == true ? AxisDirection.Backward : AxisDirection.Forward;
                ReturnMode mode = (ReturnMode)cmbReturnMode.SelectedItem;
                _controller.ReturnToZero(direction, mode);
            }
            else
            {
                MessageBox.Show("请选择回零类型!");
            }
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

        private void btnAbsMove_Click(object sender, RoutedEventArgs e)
        {
            double val = double.Parse(txtAbs.Text);
            _controller.AbsoluteMove(val);
        }

        private void btnSetLimit_Click(object sender, RoutedEventArgs e)
        {
            _controller.SetLimit(_controller.IOItems.ToList());
        }

        private void btnsSaveParam_Click(object sender, RoutedEventArgs e)
        {
            _controller.SaveParam();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            _controller.Reset();
        }

        private void chbEnable_Click(object sender, RoutedEventArgs e)
        {
            _controller.SetMotorEnable(true);
        }

        private void chbUnable_Click(object sender, RoutedEventArgs e)
        {
            _controller.SetMotorEnable(false);
        }
    }
}