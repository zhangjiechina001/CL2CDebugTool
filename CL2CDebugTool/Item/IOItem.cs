using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CL2CDebugTool.Item
{
    public enum FunctionType
    {
        ForwardLimit=0x25,
        BackLimit=0x26
    }

    public enum PolartyType
    {
        Open,
        Close
    }

    public class IOItem
    {
        public string Name { get; set; }

        public FunctionType FunctionType{ get; set; }

        public PolartyType PolartyType { get; set; }

        public bool State { get; set; }

        public List<FunctionType> FunctionTypes=>Enum.GetValues<FunctionType>().ToList();

        public List<PolartyType> PolartyTypes => Enum.GetValues<PolartyType>().ToList();
    }
}
