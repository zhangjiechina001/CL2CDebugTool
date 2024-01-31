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
            List<StateItem> items = new List<StateItem>()
            {
                new StateItem("故障",""),
                new StateItem("使能",""),
                new StateItem("运行",""),
                new StateItem("无效",""),
                new StateItem("指令完成",""),
                new StateItem("路径完成",""),
                new StateItem("回零完成",""),
                new StateItem("当前报警","0x01:过流 0x02:过压 0x40:电流采样回路故障 0x80:锁轴 0x200:EEPROM 0x100:参数自整定故障"),
                new StateItem("当前位置",""),
                new StateItem("当前速度",""),
                new StateItem("限位报警","0x100:限位故障 0x102:超程 0x20*:路径*限位故障"),
                new StateItem("DO1","0x25:正向限位 0x26:反向限位 常闭 0xA5 0xA6"),
                new StateItem("DO2","0x25:正向限位 0x26:反向限位 常闭 0xA5 0xA6"),
            };

            foreach(var name in items)
            {
                _stateItems.Add(name);
            }
            _ioItems.Add(new IOItem() { Name = "DO0" });
            _ioItems.Add(new IOItem() { Name = "DO1" });

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

        public void SetMotorEnable(bool enable)
        {
            ushort val = (ushort)(enable ? 1 : 0);
            _modbus.WriteSingleRegister(_slaveId, 0x000f, val);
        }


        public void Stop()
        {
            _modbus.WriteSingleRegister(_slaveId, 0x6002, 0x40);
        }

        public void SaveParam()
        {
            _modbus.WriteSingleRegister(_slaveId, 0x1801, 0x2211);
        }

        public void Reset()
        {
            _modbus.WriteSingleRegister(_slaveId, 0x1801, 0x2233);
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

            var limits=_modbus.ReadHoldingRegisters(_slaveId, 0x0145,3);
            _stateItems[11].State = $"{limits[0].ToString("X")}";
            _stateItems[12].State = $"{limits[2].ToString("X")}";
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
