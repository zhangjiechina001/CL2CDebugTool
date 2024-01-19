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

namespace CL2CDebugTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CL2CController _controller=new CL2CController();
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.ConnectToHost(txtAddr.Text);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _controller.Dispose();
        }
    }
}