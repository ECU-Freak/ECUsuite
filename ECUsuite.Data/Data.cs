using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECUsuite.ECU;
using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;


namespace ECUsuite.Data
{
    public class EcuData
    {
        private Tools tools = new Tools();


        /// <summary>
        /// contains the file path of the data
        /// </summary>
        private string _filePath { get; set; }
        public string filePath 
        {
            get { return _filePath; } 

            set
            {
                fileInfo = new FileInfo(value);
                _filePath = value;
            }
        }

        /// <summary>
        /// contains the file info of current file
        /// </summary>
        private FileInfo fileInfo { get; set; }

        /// <summary>
        /// returns the length of the file
        /// </summary>
        public int fileLength
        {
            get
            {
                return this.fileData.Length;
            }
        }

        /// <summary>
        /// contains the data of the file
        /// </summary>
        public byte[] fileData { get; set; } = null;

        /// <summary>
        /// contains the symbols for the ecu
        /// </summary>
        public SymbolCollection symbols { get; set; } = new SymbolCollection();

        public List<CodeBlock> codeBlocks { get; set; } = new List<CodeBlock>();


        /// <summary>
        /// contains additional infos for the ecu
        /// </summary>
        public ECUInfo info { get; set; } = new ECUInfo();


        /// <summary>
        /// extract the map contet from binary
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public SymbolData GetSymbolData(SymbolHelper symbol)
        {
            SymbolData returnData = new SymbolData();

            int xlen        = symbols.ToList().First(x => x.Varname == symbol.Varname).Xaxis.Length;
            int xaddress    = symbols.ToList().First(x => x.Varname == symbol.Varname).Xaxis.StartAddress;
            int ylen        = symbols.ToList().First(x => x.Varname == symbol.Varname).Yaxis.Length;
            int yaddress    = symbols.ToList().First(x => x.Varname == symbol.Varname).Yaxis.StartAddress;

            returnData.X_axisvalues = tools.convertBytesToInts(fileData, yaddress,ylen);
            returnData.Y_axisvalues = tools.convertBytesToInts(fileData, xaddress, xlen);

            returnData.Map_content = new byte[symbol.Length];

            Array.Copy(fileData, symbol.StartAddress, returnData.Map_content, 0, symbol.Length);
            
            return returnData;
        }

        public void SetSymbolData(SymbolHelper symbol, SymbolData data)
        {

        }



        /// <summary>
        /// read the file as byte array
        /// save it to this.fileData property
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public byte[] readFile(string filePath = null)
        {
            if(filePath == null) filePath = this.filePath;

            this.fileData = System.IO.File.ReadAllBytes(filePath);

            return this.fileData;
        }

        /// <summary>
        /// save the byte array to a file
        /// </summary>
        /// <param name="filePath"></param>
        public void saveFile(string filePath = null)
        {
            if (filePath == null) filePath = this.filePath;

            System.IO.File.WriteAllBytes(filePath, this.fileData);
        }
    }
}
