using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECUsuite.ECU.Base
{
    [MemoryPackable]
    public partial class Symbols
    {
        public List<SymbolHelper> symbolList = new List<SymbolHelper>();
    }
}
