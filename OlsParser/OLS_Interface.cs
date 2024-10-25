using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlsParser
{
    interface IOlsParser
    {
        /// <summary>
        /// extract the raw ols symbols from file data
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns></returns>
        public List<OlsSymbol>  findOlsSymbols(byte[] fileData);

        /// <summary>
        /// convert ols symbosl to ecuSuite symbol
        /// </summary>
        /// <param name="olsSymbol"></param>
        /// <returns></returns>
        public SymbolHelper     convertOlsSymbol(OlsSymbol olsSymbol);
    }
}
