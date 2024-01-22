using CL2CDebugTool.Logger;
using System.Configuration;
using System.Data;
using System.Windows;

namespace CL2CDebugTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Console.SetOut(LogTextWriter.GetInstance());
        }
    }

}
