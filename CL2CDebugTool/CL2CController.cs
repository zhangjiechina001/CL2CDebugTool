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
using NModbus.Extensions.Enron;

namespace CL2CDebugTool
{
    public enum ReturnMode
    {
        //限位回零
        Limit,
        //原点回零
        Zero
    }

    public enum AxisDirection
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
        private readonly ObservableCollection<IOItem> _ioItems = new ObservableCollection<IOItem>();

        public CL2CController() 
        {
            _tcpClient = new TcpClient();
            List<string> names = new List<string>()
            {
                "故障","使能","运行","无效","指令完成","路径完成","回零完成","当前报警","当前位置","当前速度","限位报警"
            };

            foreach (string name in names)
            {
                _stateItems.Add(new StateItem(name));
            }
            _ioItems.Add(new IOItem() { Name = "dSl1" });
            _ioItems.Add(new IOItem() { Name = "dSl2" });

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

        public ObservableCollection<IOItem> IOItems => _ioItems;

        public void ReturnToZero(AxisDirection direction, ReturnMode mode)
        {
            bool[] bArr = new bool[7];
            bArr[0] = direction == AxisDirection.Forward;
            bArr[2] = mode == ReturnMode.Zero;
            _modbus.WriteSingleRegister(_slaveId,0x600A,GetNum(bArr));
            _modbus.WriteSingleRegister(_slaveId, 0x600f, 0x0064);
            _modbus.WriteSingleRegister(_slaveId, 0x6010, 0x001e);
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x0020);
        }

        public void SetCurrentToZero()
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x21);
        }

        public void SetVel(double vel)
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6203, (ushort)(vel * 100));
        }

        public void RelativeMove(double relativeMove)
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6200, 0x41);

            int tempPos = (int)(relativeMove * 10000);
            //写入位置
            _modbus.WriteMultipleRegisters(_slaveId, 0x6201, ConvertToUint16List(tempPos).ToArray());
            //启动
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x0010);
        }

        public void AbsoluteMove(double relativeMove)
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6200, 0x01);

            int tempPos = (int)(relativeMove * 10000);
            //写入位置
            _modbus.WriteMultipleRegisters(_slaveId, 0x6201, ConvertToUint16List(tempPos).ToArray());
            //启动
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x0010);
        }

        public void SetLimit(List<IOItem> ioItem)
        {
            ushort startAddr = 0x0145;
            for (int i = 0; i < ioItem.Count; i++)
            {
                int val = ioItem[i].PolartyType == PolartyType.Open ? (int)ioItem[i].FunctionType : (int)ioItem[i].FunctionType + 0x80;
                _modbus.WriteSingleRegister(_slaveId, (ushort)(startAddr + i * 2), (ushort)val);
            }
        }

        public void SetLimit(AxisDirection direction,bool enable)
        {
            ushort limitAddr = (ushort)(direction == AxisDirection.Backward ? 0x145 : 0x147);
            int value = direction == AxisDirection.Backward ? 0x25 : 0x26;
            ushort writeVale = (ushort)(enable ? value : (value + 0x80));
            Console.WriteLine($"limitAddr:{limitAddr} writeVale:{writeVale}");
            _modbus.WriteSingleRegister(_slaveId,limitAddr, writeVale);
        }

        public void GetLimit(AxisDirection direction,ref bool enable)
        {
            ushort limitAddr = (ushort)(direction == AxisDirection.Backward ? 0x145 : 0x147);
            int value = direction == AxisDirection.Backward ? 0x25 : 0x26;
            ushort writeVale = (ushort)(enable ? value : (value + 0x80));
            Console.WriteLine($"limitAddr:{limitAddr} writeVale:{writeVale}");
            enable=(_modbus.ReadHoldingRegisters(_slaveId, limitAddr, 1).First()==value);
        }

        public void SetServorEnable(bool enable)
        {
            ushort val = (ushort)(enable ? 1 : 0);
            _modbus.WriteSingleRegister(_slaveId, 0x000f, val);
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
            var curPos = _modbus.ReadHoldingRegisters(_slaveId, 0x602c, 2);
            int pos = ConvertToInt(curPos);
            _stateItems[8].State = $"{pos / 10000.0}" ;
            var vel=_modbus.ReadHoldingRegisters(_slaveId, 0x6203, 1);
            _stateItems[9].State = $"{vel[0] /100.0}";

            var alarm = _modbus.ReadHoldingRegisters(_slaveId, 0x601d, 1).First();
            _stateItems[10].State = alarm.ToString("X");
        }

        public static bool GetBit(ushort b, int bitNumber)
        {
            if (bitNumber < 1 || bitNumber > 16)
            {
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 1 - 16");
            }
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

        public static int ConvertToInt(ushort[] data)
        {
            int data0 = data[0];
            return (data0 << 16) + data[1];
        }

        List<ushort> ConvertToUint16List(int data)
        {
            List<ushort> result = new List<ushort>();
            result.Add((ushort)(data >> 16));
            result.Add((ushort)(data & 0xFFFF));
            return result;
        }

        public void Dispose()
        {
            _modbus?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
