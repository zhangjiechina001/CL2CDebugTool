using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CL2CDebugTool.Logger
{
    public class LogTextWriter : TextWriter
    {
        public LogTextWriter()
        {
        }

        public override Encoding Encoding => Encoding.UTF8;

        public EventHandler<string> OnLogging;

        public override void Write(string? value)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string path = GetPath();
            File.AppendAllText(path,$"{timestamp} - {value}");
        }

        public override void WriteLine(string? value)
        {
            if(value!=null)
            {
                OnLogging?.Invoke(this,value);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string path = GetPath();
                File.AppendAllText(path, $"{timestamp} - {value}{Environment.NewLine}");
            }
        }

        private string GetPath()
        {
            if(!Directory.Exists("./log"))
            {
                Directory.CreateDirectory("./log");
            }
            // 获取当前日期和时间作为日志文件名的一部分
            string currentDateTime = DateTime.Now.ToString("yyyy_MM_dd");
            // 构建日志文件路径
            string logFilePath = $"log/log_{currentDateTime}.txt";
            return logFilePath;
        }

        private static LogTextWriter _instance = new LogTextWriter();
        public static LogTextWriter GetInstance()
        {
            return _instance;
        }
    }
}
