using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CL2CDebugTool.Item;

namespace CL2CDebugTool
{
    public class CL2CController : IDisposable
    {
        private IModbusMaster _modbus;
        private TcpClient _tcpClient;
        private byte _slaveId = 1;
        private readonly ObservableCollection<StateItem> _stateItems=new ObservableCollection<StateItem>();

        public CL2CController() 
        {
            _tcpClient = new TcpClient();
            List<string> names = new List<string>()
            {
                "故障","使能","运行","无效","指令完成","路径完成","回零完成","当前报警"
            };

            foreach (string name in names)
            {
                _stateItems.Add(new StateItem(name));
            }
        }

        public bool ConnectToHost(string ip)
        {
            var host = IPAddress.Parse(ip.Split(":")[0]);
            int port = int.Parse(ip.Split(":")[1]);

            _tcpClient.Connect(host, port);
            var factory = new ModbusFactory();
            _modbus = factory.CreateMaster(_tcpClient);
            return true;
        }

        public bool IsConnected => _tcpClient.Connected;

        public ObservableCollection<StateItem> StateItems => _stateItems;

        public void UpdateState()
        {
            
            ushort state=_modbus.ReadHoldingRegisters(_slaveId, 0x1003, 1).First();
            
            for (int i = 0; i < 7; i++)
            {
                bool stateb = GetBit(state, i+1);
                _stateItems[i].State = stateb ? "1" : "0";
            }
            var error = _modbus.ReadHoldingRegisters(_slaveId, 0x2203, 1).First();
            _stateItems[7].State = error.ToString("X");
            
        }

        public static bool GetBit(ushort b, int bitNumber)
        {
            if (bitNumber < 1 || bitNumber > 16)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 16");

            return (b & (1 << (bitNumber - 1))) >= 1;
        }

        public void Dispose()
        {
            _modbus?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
