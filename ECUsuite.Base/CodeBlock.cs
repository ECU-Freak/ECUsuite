using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECUsuite.ECU.Base
{
    public class CodeBlock
    {

        public GearboxType gearboxType { get; set; } = GearboxType.Manual;

        public int StartAddress { get; set; } = 0;

        public int EndAddress { get; set; } = 0;

        public int CodeID { get; set; } = 0;

        public int AddressID { get; set; } = 0;
    }
}
