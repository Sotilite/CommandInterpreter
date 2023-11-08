using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandInterpreter
{
    internal class DataEventArgs : EventArgs
    {
        public string Data { get; set; }

        public DataEventArgs(string data)
        {
            Data = data;
        }
    }
}
