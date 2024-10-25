using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlsParser
{
    /// <summary>
    /// conatinas information about an ols symbol
    /// </summary>
    public class OlsSymbol
    {
        public int startAddress { get; set; }
        public int endAddress { get; set; }
        public int length { get; set; }
        public byte[] data { get; set; } = new byte[0];

        public int addr_NameEnd { get; set; }
        public int addr_DescrEnd { get; set; }
        public int addr_IdEnd { get; set; }
        public int addr_UnitEnd { get; set; }
    }
}
