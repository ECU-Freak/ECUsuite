using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;


namespace ECUsuite.ECU.Checksum
{
    public class ECUchecksum
    {
        private Tools tools = new Tools();


        /// <summary>
        /// reads file and calculates checksum
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileType"></param>
        /// <param name="verifyOnly"></param>
        /// <returns></returns>
        public ChecksumResultDetails UpdateChecksum(string filename, EDCFileType fileType, bool verifyOnly = false)
        {
            byte[] allBytes;
            ChecksumResult res = new ChecksumResult(); ;
            ChecksumResultDetails result = new ChecksumResultDetails();

            allBytes = File.ReadAllBytes(filename);

            UpdateChecksum(filename, allBytes, fileType, verifyOnly);

            if (result.CalculationOk) result.CalculationResult = ChecksumResult.ChecksumOK;
            return result;
        }

        /// <summary>
        /// calculates checksum from byte array
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="allBytes"></param>
        /// <param name="fileType"></param>
        /// <param name="verifyOnly"></param>
        /// <returns></returns>
        public ChecksumResultDetails UpdateChecksum(string filename, byte[] allBytes, EDCFileType fileType, bool verifyOnly = false)
        {
            ChecksumResult res = new ChecksumResult(); ;
            ChecksumResultDetails result = new ChecksumResultDetails();

            switch (fileType)
            {
                case EDCFileType.EDC15P:
                case EDCFileType.EDC15P6:
                    res = CalculateEDC15PChecksum(filename, allBytes, verifyOnly, out result);
                    break;
                case EDCFileType.EDC15V:
                    // EDC15VM+ is similar to EDC15P
                    res = CalculateEDC15VMChecksum(filename, allBytes, verifyOnly, out result);
                    break;
                case EDCFileType.EDC15C:
                    //TODO: Implement EDC15C checksum routine here
                    break;
                case EDCFileType.EDC15M:
                    //TODO: Implement EDC15M checksum routine here
                    break;
                case EDCFileType.EDC16:
                    //TODO: Implement EDC16x checksum routine here
                    break;
                case EDCFileType.EDC17:
                    //TODO: Implement EDC17x checksum routine here
                    break;
                case EDCFileType.MSA15:
                case EDCFileType.MSA12:
                case EDCFileType.MSA11:
                    //TODO: Implement MSA15 checksum routine here

                    // this should be Bosch TDI V3.1 (Version 2.04)

                    /* result.TypeResult = ChecksumType.VAG_EDC15P_V41;
                     allBytes = File.ReadAllBytes(filename);
                     //allBytes = reverseEndian(allBytes);
                     MSA15_checksum msa15chks = new MSA15_checksum();

                     res = msa15chks.tdi41_checksum_search(allBytes, (uint)allBytes.Length, false);
                     result.NumberChecksumsTotal = msa15chks.ChecksumsFound;
                     result.NumberChecksumsFail = msa15chks.ChecksumsIncorrect;
                     result.NumberChecksumsOk = msa15chks.ChecksumsFound - msa15chks.ChecksumsIncorrect;
                     if (res == ChecksumResult.ChecksumOK)
                     {
                         //Console.WriteLine("Checksum matched");
                         result.CalculationOk = true;
                     }
                     else if (res == ChecksumResult.ChecksumFail)
                     {
                         Console.WriteLine("UpdateChecksum: Checksum failed " + filename);
                         if (!verifyOnly)
                         {
                             File.WriteAllBytes(filename, allBytes);
                             result.CalculationOk = true;
                             Console.WriteLine("UpdateChecksum: Checksum fixed");
                         }

                     }*/
                    break;
                case EDCFileType.MSA6:
                    //TODO: Implement MSA6 checksum routine here
                    break;
            }
            if (result.CalculationOk) result.CalculationResult = ChecksumResult.ChecksumOK;
            return result;
        }


        private ChecksumResult CalculateEDC15PChecksum(string filename, byte[] allBytes, bool verifyOnly, out ChecksumResultDetails result)
        {
            ChecksumResult res = new ChecksumResult();
            // checksum for EDC15P is implemented
            result = new ChecksumResultDetails();

            result.CalculationResult = ChecksumResult.ChecksumFail; // default = failed
            result.TypeResult = ChecksumType.VAG_EDC15P_V41;


            if (allBytes.Length != 0x80000) return res;

            if (allBytes[0x50008] == 'V' && allBytes[0x50009] == '4' && allBytes[0x5000A] == '.' && allBytes[0x5000B] == '1')
            {
                // checksum V4.1 rev.1 
                result.TypeResult = ChecksumType.VAG_EDC15P_V41;
            }
            else if (allBytes[0x58008] == 'V' && allBytes[0x58009] == '4' && allBytes[0x5800A] == '.' && allBytes[0x5800B] == '1')
            {
                // checksum V4.1 rev.2 
                result.TypeResult = ChecksumType.VAG_EDC15P_V41V2;
            }

            //allBytes = reverseEndian(allBytes);
            EDC15P_checksum chks = new EDC15P_checksum();
            if (result.TypeResult == ChecksumType.VAG_EDC15P_V41)
            {
                res = chks.tdi41_checksum_search(allBytes, (uint)allBytes.Length, false);
            }
            else
            {
                res = chks.tdi41v2_checksum_search(allBytes, (uint)allBytes.Length, false);
            }
            result.NumberChecksumsTotal = chks.ChecksumsFound;
            result.NumberChecksumsFail = chks.ChecksumsIncorrect;
            result.NumberChecksumsOk = chks.ChecksumsFound - chks.ChecksumsIncorrect;
            if (res == ChecksumResult.ChecksumOK)
            {
                Console.WriteLine("Checksum V4.1 matched");
                result.CalculationOk = true;
            }
            else if (res == ChecksumResult.ChecksumFail)
            {
                Console.WriteLine("UpdateChecksum: Checksum failed " + filename);
                if (!verifyOnly)
                {
                    File.WriteAllBytes(filename, allBytes);
                    result.CalculationOk = true;
                    Console.WriteLine("UpdateChecksum: Checksum fixed");
                }

            }
            else if (res == ChecksumResult.ChecksumTypeError)
            {
                result.TypeResult = ChecksumType.VAG_EDC15P_V41_2002;
                EDC15P_checksum chks2002 = new EDC15P_checksum();
                allBytes = File.ReadAllBytes(filename);

                //chks2002.DumpChecksumLocations("V41 2002", allBytes); // for debug info only

                ChecksumResult res2002 = chks2002.tdi41_2002_checksum_search(allBytes, (uint)allBytes.Length, false);
                result.NumberChecksumsTotal = chks2002.ChecksumsFound;
                result.NumberChecksumsFail = chks2002.ChecksumsIncorrect;
                result.NumberChecksumsOk = chks2002.ChecksumsFound - chks2002.ChecksumsIncorrect;

                if (res2002 == ChecksumResult.ChecksumOK)
                {
                    Console.WriteLine("Checksum 2002 matched " + filename);
                    result.CalculationOk = true;
                }
                else if (res2002 == ChecksumResult.ChecksumFail)
                {
                    Console.WriteLine("UpdateChecksum: Checksum 2002 failed " + filename);
                    if (!verifyOnly)
                    {
                        File.WriteAllBytes(filename, allBytes);
                        result.CalculationOk = true;
                        Console.WriteLine("UpdateChecksum: Checksum fixed");
                    }
                }
                else if (res2002 == ChecksumResult.ChecksumTypeError)
                {
                    // unknown checksum type
                    result.CalculationOk = false;
                    result.CalculationResult = ChecksumResult.ChecksumTypeError;
                    result.TypeResult = ChecksumType.Unknown;
                }
            }
            return res;
        }

        private ChecksumResult CalculateEDC15VMChecksum(string filename, byte[] allBytes, bool verifyOnly, out ChecksumResultDetails result)
        {
            ChecksumResult res = new ChecksumResult();
            result = new ChecksumResultDetails();
            result.CalculationResult = ChecksumResult.ChecksumFail; // default = failed
            result.TypeResult = ChecksumType.VAG_EDC15VM_V41;
            if (/*allBytes.Length != 0x40000 && */allBytes.Length != 0x80000 && allBytes.Length != 0x100000) return res;
            if (allBytes.Length >= 0x80000)
            {
                if (allBytes[0x50008] == 'V' && allBytes[0x50009] == '4' && allBytes[0x5000A] == '.' && allBytes[0x5000B] == '1')
                {
                    // checksum V4.1 rev.1 
                    result.TypeResult = ChecksumType.VAG_EDC15VM_V41;
                }
                else if (allBytes[0x58008] == 'V' && allBytes[0x58009] == '4' && allBytes[0x5800A] == '.' && allBytes[0x5800B] == '1')
                {
                    // checksum V4.1 rev.2 
                    result.TypeResult = ChecksumType.VAG_EDC15VM_V41V2;
                }
            }
            //allBytes = reverseEndian(allBytes);
            EDC15VM_checksum chks = new EDC15VM_checksum();
            if (result.TypeResult == ChecksumType.VAG_EDC15VM_V41)
            {
                res = chks.tdi41_checksum_search(allBytes, (uint)allBytes.Length, false);
            }
            else
            {
                res = chks.tdi41v2_checksum_search(allBytes, (uint)allBytes.Length, false);
            }
            result.NumberChecksumsTotal = chks.ChecksumsFound;
            result.NumberChecksumsFail = chks.ChecksumsIncorrect;
            result.NumberChecksumsOk = chks.ChecksumsFound - chks.ChecksumsIncorrect;
            if (res == ChecksumResult.ChecksumOK)
            {
                Console.WriteLine("Checksum V4.1 matched");
                result.CalculationOk = true;
            }
            else if (res == ChecksumResult.ChecksumFail)
            {
                Console.WriteLine("UpdateChecksum: Checksum failed " + filename);
                if (!verifyOnly)
                {
                    File.WriteAllBytes(filename, allBytes);
                    result.CalculationOk = true;
                    Console.WriteLine("UpdateChecksum: Checksum fixed");
                }

            }
            else if (res == ChecksumResult.ChecksumTypeError)
            {
                result.TypeResult = ChecksumType.VAG_EDC15VM_V41_2002;
                EDC15VM_checksum chks2002 = new EDC15VM_checksum();
                allBytes = File.ReadAllBytes(filename);
                ChecksumResult res2002 = chks2002.tdi41_2002_checksum_search(allBytes, (uint)allBytes.Length, false);
                result.NumberChecksumsTotal = chks2002.ChecksumsFound;
                result.NumberChecksumsFail = chks2002.ChecksumsIncorrect;
                result.NumberChecksumsOk = chks2002.ChecksumsFound - chks2002.ChecksumsIncorrect;

                if (res2002 == ChecksumResult.ChecksumOK)
                {
                    Console.WriteLine("Checksum 2002 matched " + filename);
                    result.CalculationOk = true;
                }
                else if (res2002 == ChecksumResult.ChecksumFail)
                {
                    Console.WriteLine("UpdateChecksum: Checksum 2002 failed " + filename);
                    if (!verifyOnly)
                    {
                        File.WriteAllBytes(filename, allBytes);
                        result.CalculationOk = true;
                        Console.WriteLine("UpdateChecksum: Checksum fixed");
                    }
                }
                else if (res2002 == ChecksumResult.ChecksumTypeError)
                {
                    // unknown checksum type
                    result.CalculationOk = false;
                    result.CalculationResult = ChecksumResult.ChecksumTypeError;
                    result.TypeResult = ChecksumType.Unknown;
                }
            }
            return res;
        }

    }
}
