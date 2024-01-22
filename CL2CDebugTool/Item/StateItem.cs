using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CL2CDebugTool.Item
{
    public class StateItem : ObservableObject
    {
        public StateItem() { }

        public StateItem(string name)
        {
            Name = name;
        }

        public string Name { get; set; } = "";

        private string _state = "";
        public string State
        {
            get { return _state; }
            set
            {
                SetProperty(ref _state, value);
            }
        }
    }
}
