using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECUsuite.Data
{
    public class SymbolData
    {
        public DataTable x_axis { get; set; }

        public DataTable y_axis { get; set; }

        public DataTable values { get; set; }

        public byte[] Map_content { get; set; } = new byte[0];
        public int[] X_axisvalues { get; set; } = new int[0];
        public int[] Y_axisvalues { get; set; } = new int[0];
    }
}
