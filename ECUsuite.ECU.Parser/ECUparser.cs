using ECUsuite.ECU.Base;
using ECUsuite.ECU.EDC15;

namespace ECUsuite.ECU.Parser
{
    public class ECUparser
    {
        public IEDCFileParser GetParserForFile(string filename, bool isPrimaryFile)
        {
            IEDCFileParser parser = null;
            EDCFileType fileType = DetermineFileType(filename, isPrimaryFile);
            switch (fileType)
            {
                case EDCFileType.EDC15P:
                    parser = new EDC15PFileParser();
                    break;
                case EDCFileType.EDC15P6:
                    parser = new EDC15P6FileParser();
                    break;
                case EDCFileType.EDC15V:
                    parser = new EDC15VFileParser();
                    break;
                case EDCFileType.EDC15C:
                    parser = new EDC15CFileParser();
                    break;
                case EDCFileType.EDC15M:
                    parser = new EDC15MFileParser();
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

        public EDCFileType DetermineFileType(string fileName, bool isPrimaryFile)
        {   
            EDCFileType fileType = EDCFileType.EDC15P; // default

            byte[] allBytes = File.ReadAllBytes(fileName);
            string boschnumber = ExtractBoschPartnumber(allBytes);


            //Console.WriteLine("Bosch number: " + boschnumber);
            partNumberConverter pnc = new partNumberConverter();
            ECUInfo info = pnc.ConvertPartnumber(boschnumber, allBytes.Length);
            if (info.EcuType.Contains("EDC15P-6"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC15P6;
                return EDCFileType.EDC15P6;
            }
            else if (info.EcuType.Contains("EDC15P"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC15P;
                return EDCFileType.EDC15P;
            }
            else if (info.EcuType.Contains("EDC15M"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC15M;
                return EDCFileType.EDC15M;
            }
            else if (info.EcuType.Contains("MSA15") || info.EcuType.Contains("EDC15V-5"))
            {
                if (isPrimaryFile) fileType = EDCFileType.MSA15;
                return EDCFileType.MSA15;
            }
            else if (info.EcuType.Contains("MSA12"))
            {
                if (isPrimaryFile) fileType = EDCFileType.MSA12;
                return EDCFileType.MSA12;
            }
            else if (info.EcuType.Contains("MSA11"))
            {
                if (isPrimaryFile) fileType = EDCFileType.MSA11;
                return EDCFileType.MSA11;
            }
            else if (info.EcuType.Contains("MSA6"))
            {
                if (isPrimaryFile) fileType = EDCFileType.MSA6;
                return EDCFileType.MSA6;
            }
            else if (info.EcuType.Contains("EDC15V"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC15V;
                return EDCFileType.EDC15V;
            }
            if (info.EcuType.Contains("EDC15C"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC15C;
                return EDCFileType.EDC15C;
            }
            else if (info.EcuType.Contains("EDC16"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC16;
                return EDCFileType.EDC16;
            }
            else if (info.EcuType.Contains("EDC17"))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC17;
                return EDCFileType.EDC17;
            }

            else if (partNumberConverter.IsEDC16Partnumber(boschnumber))
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC16;
                return EDCFileType.EDC16;
            }
            else if (boschnumber != string.Empty)
            {
                if (allBytes.Length == 1024 * 1024 * 2)
                {
                    if (isPrimaryFile) fileType = EDCFileType.EDC17;
                    return EDCFileType.EDC17;
                }
                else if (boschnumber.StartsWith("EDC17"))
                {
                    if (isPrimaryFile) fileType = EDCFileType.EDC17;
                    return EDCFileType.EDC17;
                }
                else
                {
                    if (isPrimaryFile) fileType = EDCFileType.EDC15V;
                    return EDCFileType.EDC15V;
                }
            }
            else
            {
                if (isPrimaryFile) fileType = EDCFileType.EDC16;
                return EDCFileType.EDC16; // default to EDC16???
            }
        }



    }
}
