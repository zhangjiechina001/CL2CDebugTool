using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CL2CDebugTool
{
    public class CL2CController : IDisposable
    {
        private IModbusMaster _modbus;
        private Socket _sock;
        private byte _slaveId = 1;
        
        public CL2CController() { }

        public bool ConnectToHost(string ip)
        {
            Uri uri = new Uri(ip);
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sock.Connect(uri.Host,uri.Port);

            var factory = new ModbusFactory();
            _modbus = factory.CreateMaster(_sock);
            return true;
        }

        public void Dispose()
        {
            _modbus?.Dispose();
            _sock?.Dispose();
        }
    }
}
