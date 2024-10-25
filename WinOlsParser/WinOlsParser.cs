using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;


namespace WinOlsParser
{
    public class WinOlsParser
    {
        private Tools tools = new Tools();
        private string filePath { get; set; }
        private byte[] fileData { get; set; }

        /// <summary>
        /// read .ols file and save to byte array
        /// </summary>
        /// <param name="filePath"></param>
        public void readFile(string filePath)
        {
            if (filePath == null) filePath = this.filePath;

            this.fileData = System.IO.File.ReadAllBytes(filePath);
        }


        public SymbolCollection extractSymbols()
        {
            SymbolCollection symbols = new SymbolCollection();

            findOlsSymbols(fileData);




            return symbols;
        }


        /// <summary>
        /// find raw ols symbols
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns></returns>
        private List<OlsSymbol> findOlsSymbols(byte[] fileData)
        {
            byte[] olsStartSignature = new byte[9] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x02 };
            byte[] olsEndSignature   = new byte[7] { 0x01, 0x01, 0x01, 0xFF, 0xFF, 0xFF, 0xFF };
            
            List<OlsSymbol> olsSymbols = new List<OlsSymbol>();



            OlsSymbol olsSymbol = new OlsSymbol();
            olsSymbol.startAddress  = tools.findSequence(fileData, 0, olsStartSignature);
            olsSymbol.endAddress    = tools.findSequence(fileData, 0, olsEndSignature) + olsEndSignature.Length;
            olsSymbol.length        = olsSymbol.endAddress - olsSymbol.startAddress;



            olsSymbols.Add(olsSymbol);


            return olsSymbols;
        }
    }


    public class OlsSymbol
    {
        public int startAddress { get; set; }
        public int endAddress { get; set; }
        public int length { get; set; }
        public byte[] data { get; set; }
    }


}
