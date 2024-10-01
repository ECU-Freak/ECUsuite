using ECUsuite.Data;
using ECUsuite.ECU.Base;
using ECUsuite.ECU.EDC15;

namespace ECUsuite.ECU
{
    public class ECUparser
    {

        public void ParseECU(ref EcuData data )
        {

            partNumberConverter pnc = new partNumberConverter();


            IecuFileParser parser = GetParserForFile(data.fileData);
            List<CodeBlock>     newCodeBlocks   = new List<CodeBlock>();
            List<AxisHelper>    newAxisHelpers  = new List<AxisHelper>();

            //extract bosch / conti / delphi Number
            string manufacturerNum = parser.ExtractManufacturerNumber(data.fileData);

            //extract oem number 
            string oemNumber = parser.ExtractOemNumber(data.fileData);

            //convert manufacturer number to ECUinfo
            data.info = pnc.ConvertPartnumber(manufacturerNum, data.fileData.Length);


            //parse file
            data.symbols = parser.parseFile(data.fileData, out newCodeBlocks, out newAxisHelpers);


        }


        public IecuFileParser GetParserForFile(byte[] binaryFile)
        {
            IecuFileParser parser = null;

            EDCFileType fileType = DetermineFileType(binaryFile);

            switch (fileType)
            {
                case EDCFileType.EDC15P:
                    parser = new EDC15P_FileParser();
                    break;
                case EDCFileType.EDC15P6:
                    //parser = new EDC15P6FileParser();
                    break;
                case EDCFileType.EDC15V:
                    //parser = new EDC15VFileParser();
                    break;
                case EDCFileType.EDC15C:
                    //parser = new EDC15CFileParser();
                    break;
                case EDCFileType.EDC15M:
                    //parser = new EDC15MFileParser();
                    break;
                case EDCFileType.EDC16:
                    //parser = new ECU.EDC16.EDC16FileParser();
                    break;
                case EDCFileType.EDC17:
                    //parser = new ECU.EDC17.EDC17FileParser();
                    break;
                case EDCFileType.MSA15: //?
                case EDCFileType.MSA12:
                case EDCFileType.MSA11:
                    //parser = new ECU.MSA15.MSA15FileParser();
                    break;
                case EDCFileType.MSA6:
                    //parser = new ECU.MSA6.MSA6FileParser();
                    break;
            }
            return parser;
        }

        /// <summary>
        /// Determines the ECU
        /// </summary>
        /// <param name="binaryFile"></param>
        /// <returns></returns>
        public EDCFileType DetermineFileType(byte[] binaryFile)
        {
            EDC15P_FileParser parser = new EDC15P_FileParser();
            partNumberConverter pnc = new partNumberConverter();

            EDCFileType fileType = EDCFileType.EDC15P; // default

            string boschnumber = parser.ExtractManufacturerNumber(binaryFile);

            ECUInfo info = pnc.ConvertPartnumber(boschnumber, binaryFile.Length);


            if (info.EcuType.Contains("EDC15P-6"))
            {
                fileType = EDCFileType.EDC15P6;
                return EDCFileType.EDC15P6;
            }
            else if (info.EcuType.Contains("EDC15P"))
            {
                fileType = EDCFileType.EDC15P;
                return EDCFileType.EDC15P;
            }
            else if (info.EcuType.Contains("EDC15M"))
            {
                fileType = EDCFileType.EDC15M;
                return EDCFileType.EDC15M;
            }
            else if (info.EcuType.Contains("MSA15") || info.EcuType.Contains("EDC15V-5"))
            {
                fileType = EDCFileType.MSA15;
                return EDCFileType.MSA15;
            }
            else if (info.EcuType.Contains("MSA12"))
            {
                fileType = EDCFileType.MSA12;
                return EDCFileType.MSA12;
            }
            else if (info.EcuType.Contains("MSA11"))
            {
                fileType = EDCFileType.MSA11;
                return EDCFileType.MSA11;
            }
            else if (info.EcuType.Contains("MSA6"))
            {
                fileType = EDCFileType.MSA6;
                return EDCFileType.MSA6;
            }
            else if (info.EcuType.Contains("EDC15V"))
            {
                fileType = EDCFileType.EDC15V;
                return EDCFileType.EDC15V;
            }
            if (info.EcuType.Contains("EDC15C"))
            {
                fileType = EDCFileType.EDC15C;
                return EDCFileType.EDC15C;
            }
            else if (info.EcuType.Contains("EDC16"))
            {
                fileType = EDCFileType.EDC16;
                return EDCFileType.EDC16;
            }
            else if (info.EcuType.Contains("EDC17"))
            {
                fileType = EDCFileType.EDC17;
                return EDCFileType.EDC17;
            }
            else if (partNumberConverter.IsEDC16Partnumber(boschnumber))
            {
                fileType = EDCFileType.EDC16;
                return EDCFileType.EDC16;
            }
            else if (boschnumber != string.Empty)
            {
                if (binaryFile.Length == 1024 * 1024 * 2)
                {
                    fileType = EDCFileType.EDC17;
                    return EDCFileType.EDC17;
                }
                else if (boschnumber.StartsWith("EDC17"))
                {
                    fileType = EDCFileType.EDC17;
                    return EDCFileType.EDC17;
                }
                else
                {
                    fileType = EDCFileType.EDC15V;
                    return EDCFileType.EDC15V;
                }
            }
            else
            {
                fileType = EDCFileType.EDC16;
                return EDCFileType.EDC16; // default to EDC16???
            }
        }
    }
}
