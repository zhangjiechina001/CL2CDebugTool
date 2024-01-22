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
    public enum ReturnMode
    {
        //限位回零
        Limit,
        //原点回零
        Zero
    }

    public enum RetrunZeroDirection
    {
        Forward,
        Backward
    }

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

        public void ReturnToZero(RetrunZeroDirection direction, ReturnMode mode)
        {
            bool[] bArr = new bool[7];
            bArr[0] = direction == RetrunZeroDirection.Forward;
            bArr[2] = mode == ReturnMode.Zero;
            Convert.ToUInt16(bArr);
            _modbus.WriteSingleRegister(_slaveId,0x600A,GetNum(bArr));
            _modbus.WriteSingleRegister(_slaveId, 0x600f, 0x0064);
            _modbus.WriteSingleRegister(_slaveId, 0x6010, 0x001e);
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x20);
        }

        public void SetCurrentToZero()
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x21);
        }

        public void Stop()
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x40);
        }


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

        public static ushort GetNum(bool[] bArr)
        {
            ushort result = 0;

            for (int i = 0; i < bArr.Length; i++)
            {
                if (bArr[i])
                {
                    // 如果当前布尔值为true，则将对应的位设置为1
                    result |= (ushort)(1 << i);
                }
            }
            return result;
        }

        public void Dispose()
        {
            _modbus?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
