// Todo: find cowFUN_AGR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ECUsuite.Toolbox;
using ECUsuite.ECU.Base;
using ECUsuite.ECU;
using System.Data.SqlTypes;

namespace ECUsuite.ECU.EDC15
{
    public class EDC15P_FileParser : IecuFileParser
    {
        private Tools tools = new Tools();
        //private partNumberConverter pnc = new partNumberConverter();


        #region extract ecu infos
        public string ExtractInfo(byte[] binaryData)
        {
            // assume info will be @ 0x53452 12 bytes
            string retval = string.Empty;
            try
            {
                int partnumberAddress = tools.findSequence(binaryData, 0, new byte[5] { 0x45, 0x44, 0x43, 0x20, 0x20 }, new byte[5] { 1, 1, 1, 1, 1 });
                if (partnumberAddress > 0)
                {
                    retval = Encoding.ASCII.GetString(binaryData, partnumberAddress - 8, 12).Trim();
                }
            }
            catch (Exception)
            {
            }
            return retval;
        }

        public string ExtractOemNumber(byte[] binaryData)
        {
            // assume info will be @ 0x53446 12 bytes
            string retval = string.Empty;
            try
            {
                int partnumberAddress = tools.findSequence(binaryData, 0, new byte[5] { 0x45, 0x44, 0x43, 0x20, 0x20 }, new byte[5] { 1, 1, 1, 1, 1 });
                if (partnumberAddress > 0)
                {
                    retval = Encoding.ASCII.GetString(binaryData, partnumberAddress - 20, 12).Trim();
                }
            }
            catch (Exception)
            {
            }
            return retval;
        }

        public string ExtractManufacturerNumber(byte[] binaryData)
        {
            string retval = string.Empty;
            try
            {
                int partnumberAddress = tools.findSequence(binaryData, 0, new byte[5] { 0x45, 0x44, 0x43, 0x20, 0x20 }, new byte[5] { 1, 1, 1, 1, 1 });
                if (partnumberAddress > 0)
                {
                    // for EDC
                    retval = System.Text.ASCIIEncoding.ASCII.GetString(binaryData, partnumberAddress + 23, 10).Trim();
                    if (tools.StripNonAscii(retval).Length < 10)
                    {
                        // try again, read from "EDC" id - 0x100 to EDC id + 100 and find 10 digit sequence
                        retval = tools.FindDigits(binaryData, partnumberAddress - 0x100, partnumberAddress + 0x100, 10);
                    }
                    if (retval == string.Empty)
                    {
                        // try EDC16, other partnumber struct
                        retval = tools.FindAscii(binaryData, partnumberAddress - 0x100, partnumberAddress + 0x100, 11);
                        if (retval.StartsWith("ECM"))
                        {
                            retval = tools.FindAscii(binaryData, partnumberAddress - 0x100, partnumberAddress + 0x100, 19);
                        }
                    }
                }
                else
                {
                    partnumberAddress = tools.findSequence(binaryData, 0, new byte[4] { 0x30, 0x32, 0x38, 0x31 }, new byte[4] { 1, 1, 1, 1 });
                    if (partnumberAddress > 0)
                    {
                        retval = System.Text.ASCIIEncoding.ASCII.GetString(binaryData, partnumberAddress, 10).Trim();
                    }
                }
                if (retval == string.Empty)
                {
                    partnumberAddress = tools.findSequence(binaryData, 0, new byte[4] { 0x30, 0x32, 0x38, 0x31 }, new byte[4] { 1, 1, 1, 1 });
                    if (partnumberAddress > 0)
                    {
                        retval = System.Text.ASCIIEncoding.ASCII.GetString(binaryData, partnumberAddress, 10).Trim();
                    }
                }
                if (retval == string.Empty)
                {
                    // check 512 kB EDC17 file, audi Q7 3.0TDI
                    partnumberAddress = tools.findSequence(binaryData, 0, new byte[7] { 0x45, 0x44, 0x43, 0x31, 0x37, 0x20, 0x20 }, new byte[7] { 1, 1, 1, 1, 1, 1, 1 });
                    if (partnumberAddress > 0)
                    {
                        retval = "EDC17" + System.Text.ASCIIEncoding.ASCII.GetString(binaryData, partnumberAddress - 68, 10).Trim();
                    }
                    if (retval == string.Empty)
                    {
                        //EDC17_CPx4 = Audi
                        //EDC17_CPx2 = BMW?
                        partnumberAddress = tools.findSequence(binaryData, 0, Encoding.ASCII.GetBytes("EDC17_"), new byte[6] { 1, 1, 1, 1, 1, 1 });
                        if (partnumberAddress > 0)
                        {
                            retval = "EDC17";
                        }
                    }


                }
                if (retval == string.Empty)
                {

                    // 1. General lookup for EDC17-string in file
                    int pos = tools.findSequence(binaryData, 0, Encoding.ASCII.GetBytes("ME(D)/EDC17"), new byte[11] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
                    if (pos > 0)
                    {
                        Console.WriteLine("Found ME(D)/EDC17 in firmware!");
                        retval = "EDC17";
                    }
                    // 2. BMW has 0281xxxxxx (boschnumber) written at FD00 and FE33
                    // 3. BMW has 10373xxxxx (softwarenumber)written at 0x4001A, use this as a backup if boschnumber can't be found

                    StringBuilder sb1 = new StringBuilder();
                    StringBuilder sb2 = new StringBuilder();
                    bool identified = true;
                    // Check1
                    for (int offset = 0xFD00; offset < 0xFD00 + 10; offset++)
                    {
                        byte b = binaryData[offset];
                        b -= 0x30;
                        if (b < 0 || b > 9)
                            identified = false;
                        sb1.Append(b.ToString());
                    }
                    for (int offset = 0xFE33; offset < 0xFE33 + 10; offset++)
                    {
                        byte b = binaryData[offset];
                        b -= 0x30;
                        if (b < 0 || b > 9)
                            identified = false;
                        sb2.Append(b.ToString());
                    }
                    //Console.WriteLine("sb1=" + sb1.ToString());
                    //Console.WriteLine("sb2=" + sb2.ToString());
                    if (identified && sb1.ToString() == sb2.ToString())
                    {
                        partnumberAddress = 0xFD00;
                        retval = sb1.ToString();
                    }

                    StringBuilder sb3 = new StringBuilder();
                    identified = true;
                    // Check2
                    for (int offset = 0x4001A; offset < 0x4001A + 10; offset++)
                    {
                        byte b = binaryData[offset];
                        b -= 0x30;
                        if (b < 0 || b > 9)
                            identified = false;
                        sb3.Append(b.ToString());
                    }
                    //Console.WriteLine("sb3=" + sb3.ToString());
                    if (identified)
                    {
                        partnumberAddress = 0x4001A;
                        retval = sb3.ToString();
                    }
                }
            }
            catch (Exception)
            {
            }
            retval = tools.StripNonAscii(retval);
            return retval;
        }

        public string ExtractSoftwareNumber(byte[] binaryData)
        {
            string retval = string.Empty;
            try
            {
                int partnumberAddress = tools.findSequence(binaryData, 0, new byte[5] { 0x45, 0x44, 0x43, 0x20, 0x20 }, new byte[5] { 1, 1, 1, 1, 1 });
                if (partnumberAddress > 0)
                {
                    retval = Encoding.ASCII.GetString(binaryData, partnumberAddress + 5, 8).Trim();
                    retval = retval.Replace(" ", "");
                }
            }
            catch (Exception)
            {
            }
            return retval;
        }
        #endregion

        private string DetermineNumberByFlashBank(long address, List<CodeBlock> currBlocks)
        {
            foreach (CodeBlock cb in currBlocks)
            {
                if (cb.StartAddress <= address && cb.EndAddress >= address)
                {
                    //  if (cb.CodeID == 1) return "codeblock 1";// - MAN";
                    //  if (cb.CodeID == 2) return "codeblock 2";// - AUT (hydr)";
                    //  if (cb.CodeID == 3) return "codeblock 3";// - AUT (elek)";
                    //  return cb.CodeID.ToString();
                    if (cb.gearboxType == GearboxType.Automatic)
                    {
                        return "codeblock " + cb.CodeID.ToString() + ", automatic";
                    }
                    else if (cb.CodeID == 2) return "codeblock " + cb.CodeID.ToString() + ", manual";
                    else if (cb.CodeID == 3) return "codeblock " + cb.CodeID.ToString() + ", 4x4";
                    return "codeblock " + cb.CodeID.ToString();
                }
            }
            long bankNumber = address / 0x10000;
            return "flashbank " + bankNumber.ToString();
        }

        private int DetermineCodeBlockByByAddress(long address, List<CodeBlock> currBlocks)
        {
            foreach (CodeBlock cb in currBlocks)
            {
                if (cb.StartAddress <= address && cb.EndAddress >= address)
                {
                    return cb.CodeID;
                }
            }
            return 0;
        }

        public SymbolCollection parseFile(byte[] allBytes, out List<CodeBlock> CodeBlocks, out List<AxisHelper> newAxisHelpers)
        {
            CodeBlocks = new List<CodeBlock>();
            newAxisHelpers = new List<AxisHelper>();
            SymbolCollection newSymbols = new SymbolCollection();

            serachCodeBlocks(allBytes, CodeBlocks);

            for (int t = 0; t < allBytes.Length - 1; t += 2)
            {
                int len2skip = 0;
                //if (t == 0x4dc26) Console.WriteLine("ho");
                if (CheckMap(t, allBytes, newSymbols, CodeBlocks, out len2skip))
                {
                    int from = t;
                    if (len2skip > 2) len2skip -= 2; // make sure we don't miss maps
                    if (len2skip % 2 > 0) len2skip -= 1;
                    if (len2skip < 0) len2skip = 0;
                    t += len2skip;
                }
            }

            newSymbols.SortColumn = "StartAddress";
            newSymbols.SortingOrder = GenericComparer.SortOrder.Ascending;
            newSymbols.Sort();


            NameKnownMaps(allBytes, newSymbols, CodeBlocks);

            BuildAxisIDList(newSymbols, newAxisHelpers);
            MatchAxis(newSymbols, newAxisHelpers);

            RemoveNonSymbols(newSymbols, CodeBlocks);
            FindSVBL(allBytes, newSymbols, CodeBlocks);
            SymbolTranslator strans = new SymbolTranslator();
            foreach (SymbolHelper sh in newSymbols)
            {
                sh.Description = strans.TranslateSymbolToHelpText(sh.Varname);
            }
            // check for must have maps... if there are maps missing, report it
            return newSymbols;
        }

        private void MatchAxis(SymbolCollection newSymbols, List<AxisHelper> newAxisHelpers)
        {
            foreach (SymbolHelper sh in newSymbols)
            {
                if (!sh.Yaxis.assigned)
                {
                    foreach (AxisHelper ah in newAxisHelpers)
                    {
                        if (sh.Xaxis.ID == ah.AxisID)
                        {
                            sh.Yaxis.Description = ah.Description;
                            sh.Yaxis.Unit = ah.Units;
                            sh.Yaxis.factor.offset = ah.Offset;
                            sh.Yaxis.factor.factor = ah.Correction;
                            break;
                        }
                    }
                }
                if (!sh.Xaxis.assigned)
                {
                    foreach (AxisHelper ah in newAxisHelpers)
                    {
                        if (sh.Yaxis.ID == ah.AxisID)
                        {
                            sh.Xaxis.Description = ah.Description;
                            sh.Xaxis.Unit = ah.Units;
                            sh.Xaxis.factor.offset = ah.Offset;
                            sh.Xaxis.factor.factor = ah.Correction;
                            break;
                        }
                    }
                }

            }
        }

        private void BuildAxisIDList(SymbolCollection newSymbols, List<AxisHelper> newAxisHelpers)
        {
            foreach (SymbolHelper sh in newSymbols)
            {
                if (!sh.Varname.StartsWith("2D") && !sh.Varname.StartsWith("3D"))
                {
                    AddToAxisCollection(newAxisHelpers, sh.Yaxis.ID, sh.Xaxis.Description, sh.Xaxis.Unit, sh.Xaxis.factor.factor, sh.Xaxis.factor.offset);
                    AddToAxisCollection(newAxisHelpers, sh.Xaxis.ID, sh.Yaxis.Description, sh.Yaxis.Unit, sh.Yaxis.factor.factor, sh.Yaxis.factor.offset);
                }
            }
        }

        private void AddToAxisCollection(List<AxisHelper> newAxisHelpers, int ID, string descr, string units, double factor, double offset)
        {
            if (ID == 0) return;
            foreach (AxisHelper ah in newAxisHelpers)
            {
                if (ah.AxisID == ID) return;
            }
            AxisHelper ahnew = new AxisHelper();
            ahnew.AxisID = ID;
            ahnew.Description = descr;
            ahnew.Units = units;
            ahnew.Correction = factor;
            ahnew.Offset = offset;
            newAxisHelpers.Add(ahnew);
        }

        private void RemoveNonSymbols(SymbolCollection newSymbols, List<CodeBlock> newCodeBlocks)
        {
            if (newCodeBlocks.Count > 0)
            {
                foreach (SymbolHelper sh in newSymbols)
                {
                    if (sh.CodeBlock == 0 && (sh.Varname.StartsWith("2D") || sh.Varname.StartsWith("3D")))
                    {
                        sh.Subcategory = "Zero codeblock stuff";

                    }
                }
            }
        }

        public void NameKnownMaps(byte[] allBytes, SymbolCollection newSymbols, List<CodeBlock> newCodeBlocks)
        {
            foreach (SymbolHelper sh in newSymbols)
            {
                if (sh.Length == 700) // 25*14
                {
                    sh.Category = "Detected maps";
                    sh.Subcategory = "Misc";
                    sh.Varname = "Launch control map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                    sh.Yaxis.factor.factor = 0.156250;
                    //sh.Yaxis.factor.factor = 0.000039;
                    sh.factor.factor = 0.01;
                    sh.Xaxis.Description = "Engine speed (rpm)";
                    //sh.Yaxis.Description = "Ratio vehicle/engine speed";
                    sh.Yaxis.Description = "Approx. vehicle speed (km/h)";
                    //sh.Description = "Output percentage";
                    sh.Description = "IQ limit";
                    sh.Yaxis.Unit = "km/h";
                    sh.Xaxis.Unit = "rpm";
                }
                else if (sh.Length == 570)
                {
                    if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {

                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";
                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC4 && sh.Yaxis.ID / 256 == 0xEA)
                    {

                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC4 && sh.Yaxis.ID / 256 == 0xEC)
                    {

                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }
                }
                else if (sh.Length == 480)
                {
                    if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {

                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;
                        //IAT, ECT or Fuel temp?

                        double tempRange = GetTemperatureDurRange(injDurCount - 1);
                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";
                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC4 && sh.Yaxis.ID / 256 == 0xEA)
                    {

                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;
                        //IAT, ECT or Fuel temp?

                        double tempRange = GetTemperatureDurRange(injDurCount - 1);
                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";
                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }

                }
                else if (sh.Length == 448)
                {
                    if (sh.MapSelector.NumRepeats == 10)
                    {
                        // SOI maps detected
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Start of injection (SOI)", sh.CodeBlock, newSymbols, false);

                        //based on coolant temperature
                        double tempRange = GetTemperatureSOIRange(sh.MapSelector, injDurCount - 1);
                        sh.Varname = "Start of injection (SOI) " + tempRange.ToString() + " °C [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");

                        sh.factor.factor = -0.023437;
                        sh.factor.offset = 78;

                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.factor.factor = 0.01; // TODODONE : Check for x or y
                        sh.Xaxis.Unit = "mg/st";

                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Description = "Start position (degrees BTDC)";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";

                    }
                }
                else if (sh.Length == 416)
                {
                    string strAddrTest = sh.StartAddress.ToString("X8");
                    if (sh.Xaxis.ID / 256 == 0xF9 && sh.Yaxis.ID / 256 == 0xDA)
                    {
                        // this is IQ by MAF limiter!
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        int smokeCount = GetMapNameCountForCodeBlock("Smoke limiter", sh.CodeBlock, newSymbols, false);
                        //sh.Varname = "Smoke limiter " + smokeCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Varname = "Smoke limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        if (sh.MapSelector != null)
                        {
                            if (sh.MapSelector.MapIndexes != null)
                            {
                                if (sh.MapSelector.MapIndexes.Length > 1)
                                {
                                    if (!MapSelectorIndexEmpty(sh))
                                    {
                                        double tempRange = GetTemperatureSOIRange(sh.MapSelector, smokeCount - 1);
                                        sh.Varname = "Smoke limiter " + tempRange.ToString() + " °C [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                                    }
                                }
                            }
                        }
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Xaxis.Description = "Airflow mg/stroke";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.1;
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";

                    }
                    else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xDA)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        // if x axis = upto 3000 -> MAP limit, not MAF limit
                        if (GetMaxAxisValue(allBytes, sh, AxisIdent.Y_Axis) < 4000)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Limiters";
                            sh.Varname = "IQ by MAP limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") +" " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                            sh.factor.factor = 0.01;
                            sh.Xaxis.Description = "Boost pressure";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Description = "Maximum IQ (mg)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mbar";

                        }
                        else
                        {
                            int iqMAFLimCount = GetMapNameCountForCodeBlock("IQ by MAF limiter", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "IQ by MAF limiter " + iqMAFLimCount.ToString() + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            //sh.Varname = "IQ by MAF limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Description = "Maximum IQ (mg)";
                            sh.factor.factor = 0.01;
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.Unit = "mg/st";
                            sh.Xaxis.Description = "Airflow mg/stroke";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                        }

                    }
                    else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xEA)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Turbo";
                        sh.Varname = "N75 duty cycle [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Duty cycle %";
                        sh.factor.factor = -0.01;
                        sh.factor.offset = 100;
                        //sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }
                    /*else if (strAddrTest.EndsWith("116"))
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                        sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.factor.factor = 0.1;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Description = "Mass Air Flow (mg/stroke)";
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }*/
                    else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID == 0xE9D4)
                    {
                        // x axis should start with 0!
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Turbo";
                        sh.Varname = "N75 duty cycle [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Duty cycle %";
                        sh.factor.factor = -0.01;
                        sh.factor.offset = 100;
                        //sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xEC && (sh.Yaxis.ID / 256 == 0xC0 || sh.Yaxis.ID / 256 == 0xE9))
                    {
                        // x axis should start with 0!
                        if (allBytes[sh.Yaxis.StartAddress] == 0 && allBytes[sh.Yaxis.StartAddress + 1] == 0)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.1;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Description = "Mass Air Flow (mg/stroke)";
                            sh.Xaxis.Description = "IQ (mg/stroke)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";
                        }
                    }
                    else if (sh.Xaxis.ID / 256 == 0xEA && sh.Yaxis.ID / 256 == 0xE9)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Start of injection (SOI)", sh.CodeBlock, newSymbols, false);
                        //IAT, ECT or Fuel temp?
                        double tempRange = GetTemperatureSOIRange(sh.MapSelector, injDurCount - 1);
                        sh.Varname = "Start of injection (SOI) " + tempRange.ToString() + " °C [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.factor.factor = -0.023437;
                        sh.factor.offset = 78;

                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.factor.factor = 0.01; // TODODONE : Check for x or y
                        sh.Xaxis.Unit = "mg/st";

                        sh.Description = "Start position (degrees BTDC)";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xEA && sh.Yaxis.ID / 256 == 0xE8)
                    {
                        // EGR or N75
                        if (allBytes[sh.Xaxis.StartAddress] == 0 && allBytes[sh.Xaxis.StartAddress + 1] == 0)
                        {
                            // EGR
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.1;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Description = "Mass Air Flow (mg/stroke)";
                            sh.Xaxis.Description = "IQ (mg/stroke)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";

                        }
                        else
                        {
                            //N75
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Turbo";
                            sh.Varname = "N75 duty cycle [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Description = "Duty cycle %";
                            sh.factor.factor = -0.01;
                            sh.factor.offset = 100;
                            //sh.factor.factor = 0.01;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Xaxis.Description = "IQ (mg/stroke)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";

                        }
                    }
                    /* else if ((sh.Xaxis.ID / 256 == 0xEA) && (sh.Yaxis.ID / 256 == 0xE8))
                     {
                         // x axis should start with 0!
                         if (allBytes[sh.Yaxis.StartAddress] == 0 && allBytes[sh.Yaxis.StartAddress + 1] == 0)
                         {
                             sh.Category = "Detected maps";
                             sh.Subcategory = "Misc";
                             int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                             sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                             sh.factor.factor = 0.1;
                             sh.Xaxis.factor.factor = 0.01;
                             sh.Description = "Mass Air Flow (mg/stroke)";
                             sh.Xaxis.Description = "IQ (mg/stroke)";
                             sh.Yaxis.Description = "Engine speed (rpm)";
                             sh.Yaxis.Unit = "rpm";
                             sh.Xaxis.Unit = "mg/st";
                         }
                     }*/
                }
                else if (sh.Length == 390)
                {
                    // 15x12 = inj dur limiter on R3 files
                    if (sh.Xaxis.Length == 13 && sh.Yaxis.Length == 15)
                    {
                        /* sh.Category = "Detected maps";
                         sh.Subcategory = "Limiters";
                         sh.Varname = "Injection duration limiter B [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                         sh.factor.factor = 0.023438;
                         sh.Yaxis.factor.factor = 0.01;
                         sh.Yaxis.Description = "IQ (mg/stroke)";
                         sh.Description = "Max. degrees";
                         sh.Xaxis.Description = "Engine speed (rpm)";
                         sh.Xaxis.Unit = "rpm";
                         sh.Yaxis.Unit = "mg/st";*/
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }

                    else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                        sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.factor.factor = 0.1;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Description = "Mass Air Flow (mg/stroke)";
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }

                }
                else if (sh.Length == 384)
                {
                    if (sh.Xaxis.Length == 12 && sh.Yaxis.Length == 16)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        sh.Varname = "Inverse driver wish [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Description = "Throttle  position";
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        //sh.Description = "Requested IQ (mg)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.Length == 16 && sh.Yaxis.Length == 12)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEA && sh.Yaxis.ID / 256 == 0xDA)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Limiters";
                            int smokeCount = GetMapNameCountForCodeBlock("Smoke limiter", sh.CodeBlock, newSymbols, false);
                            //sh.Varname = "Smoke limiter " + smokeCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Varname = "Smoke limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            if (sh.MapSelector != null)
                            {
                                if (sh.MapSelector.MapIndexes != null)
                                {
                                    if (sh.MapSelector.MapIndexes.Length > 1)
                                    {
                                        if (!MapSelectorIndexEmpty(sh))
                                        {
                                            double tempRange = GetTemperatureSOIRange(sh.MapSelector, smokeCount - 1);
                                            sh.Varname = "Smoke limiter " + tempRange.ToString() + " °C [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                                        }
                                    }
                                }
                            }
                            sh.Description = "Maximum IQ (mg)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Xaxis.Description = "Airflow mg/stroke";
                            sh.factor.factor = 0.01;
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";
                        }
                        else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.1;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Description = "Mass Air Flow (mg/stroke)";
                            sh.Xaxis.Description = "IQ (mg/stroke)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";
                        }
                    }
                }
                else if (sh.Length == 360)
                {
                    // 15x12 = inj dur limiter on R3 files
                    if (sh.Xaxis.Length == 12 && sh.Yaxis.Length == 15)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }

                }
                else if (sh.Length == 352)
                {
                    if (sh.Xaxis.Length == 16 && sh.Yaxis.Length == 11)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            int egrCount = GetMapNameCountForCodeBlock("EGR", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "EGR " + egrCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.1;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Description = "Mass Air Flow (mg/stroke)";
                            sh.Xaxis.Description = "IQ (mg/stroke)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";
                        }
                        else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xEA)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Turbo";
                            sh.Varname = "N75 duty cycle [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Description = "Duty cycle %";
                            sh.factor.factor = -0.01;
                            sh.factor.offset = 100;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Xaxis.Description = "IQ (mg/stroke)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "mg/st";
                        }
                    }
                }
                else if (sh.Length == 320)
                {
                    sh.Category = "Probable maps";
                    sh.Subcategory = "Turbo";
                    sh.Varname = "Boost map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "] " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                    if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                    {
                        sh.Category = "Detected maps";
                        sh.Varname = "Boost target map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Description = "Boost target (mbar)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xEA && sh.Yaxis.ID / 256 == 0xC0)
                    {
                        sh.Category = "Detected maps";
                        sh.Varname = "Boost target map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Description = "Boost target (mbar)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }
                    if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xDA)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "IQ by MAP limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") +" " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.factor.factor = 0.01;
                        sh.Xaxis.Description = "Boost pressure";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mbar";
                    }

                }
                else if (sh.Length == 308)
                {
                    sh.Category = "Detected maps";
                    sh.Subcategory = "Limiters";
                    //sh.Varname = "Boost limiter (temperature) [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                    sh.Varname = "SOI limiter (temperature) [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                    sh.factor.factor = -0.023437;
                    sh.factor.offset = 78;
                    sh.Yaxis.Description = "Engine speed (rpm)";
                    sh.Xaxis.Description = "Temperature"; //IAT, ECT or Fuel temp?
                    sh.Xaxis.factor.factor = 0.1;
                    sh.Xaxis.factor.offset = -273.1;
                    sh.Description = "SOI limit (degrees)";
                    sh.Yaxis.Unit = "rpm";
                    sh.Xaxis.Unit = "°C";
                }
                else if (sh.Length == 286)
                {
                    if (sh.Xaxis.Length == 0x0d && sh.Yaxis.Length == 0x0b)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        sh.Varname = "Driver wish [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "Throttle  position";
                        sh.Description = "Requested IQ (mg)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "TPS %";
                    }
                }
                else if (sh.Length == 280) // boost target can be 10x14 as well in Seat maps
                {
                    if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Turbo";
                        sh.Varname = "Boost target map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "] " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Description = "Boost target (mbar)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "mg/st";
                    }
                }
                else if (sh.Length == 260) // EXPERIMENTAL
                {
                    if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";

                    }
                }
                else if (sh.Length == 256)
                {
                    sh.Category = "Detected maps";
                    sh.Subcategory = "Misc";
                    sh.Varname = "Driver wish [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                    sh.factor.factor = 0.01;
                    sh.Xaxis.factor.factor = 0.01;
                    sh.Xaxis.Description = "Throttle  position";
                    sh.Description = "Requested IQ (mg)";
                    sh.Yaxis.Description = "Engine speed (rpm)";
                    sh.Yaxis.Unit = "rpm";
                    sh.Xaxis.Unit = "TPS %";

                }
                else if (sh.Length == 240)
                {
                    if (sh.Xaxis.Length == 12 && sh.Yaxis.Length == 10)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            sh.Varname = "Driver wish [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.01;
                            sh.Xaxis.factor.factor = 0.01;
                            sh.Xaxis.Description = "Throttle  position";
                            sh.Description = "Requested IQ (mg)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "TPS %";
                        }
                    }

                }
                else if (sh.Length == 220) // EXPERIMENTAL
                {
                    if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";

                    }
                }
                else if (sh.Length == 216)
                {
                    if (sh.Xaxis.Length == 12 && sh.Yaxis.Length == 9)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        sh.Varname = "Driver wish [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "Throttle  position";
                        sh.Description = "Requested IQ (mg)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "TPS %";
                    }

                }
                else if (sh.Length == 200)
                {
                    if (sh.Xaxis.ID / 256 == 0xC0 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Boost limit map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        //   sh.factor.factor = 0.01;
                        //sh.Xaxis.factor.factor = 0.01;
                        sh.Yaxis.Description = "Atmospheric pressure (mbar)";
                        sh.Description = "Maximum boost pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC0 && sh.Yaxis.ID / 256 == 0xEA)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Boost limit map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                        //   sh.factor.factor = 0.01;
                        //sh.Xaxis.factor.factor = 0.01;
                        sh.Yaxis.Description = "Atmospheric pressure (mbar)";
                        sh.Description = "Maximum boost pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        //if (!MapContainsNegativeValues(allBytes, sh))
                        if (GetMaxAxisValue(allBytes, sh, ECUsuite.ECU.Base.AxisIdent.X_Axis) > 3500) // was 5000
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel"; // was Limiters
                            // was limiter
                            int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                            injDurCount--;

                            sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                            sh.factor.factor = 0.023438;
                            sh.Yaxis.factor.factor = 0.01;
                            sh.Yaxis.Description = "IQ (mg/stroke)";
                            //sh.Description = "Max. degrees";
                            sh.Description = "Duration (crankshaft degrees)";

                            sh.Xaxis.Description = "Engine speed (rpm)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mg/st";

                        }
                        else
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                            injDurCount--;
                            //if (injDurCount < 1) injDurCount = 1;
                            sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Yaxis.factor.factor = 0.01;
                            sh.factor.factor = 0.023437;
                            sh.Xaxis.Description = "Engine speed (rpm)";
                            //sh.Yaxis.Description = "Airflow mg/stroke";
                            sh.Yaxis.Description = "Requested Quantity mg/stroke";

                            sh.Description = "Duration (crankshaft degrees)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mg/st";

                        }
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC4 && sh.Yaxis.ID / 256 == 0xEA)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;
                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }
                    else if (sh.Xaxis.ID / 256 == 0xC4 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;
                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";
                    }
                }
                else if (sh.Length == 198) // EXPERIMENTAL
                {
                    if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Fuel";
                        int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                        injDurCount--;
                        //if (injDurCount < 1) injDurCount = 1;

                        sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Yaxis.factor.factor = 0.01;
                        sh.factor.factor = 0.023437;
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        //sh.Yaxis.Description = "Airflow mg/stroke";
                        sh.Yaxis.Description = "Requested Quantity mg/stroke";

                        sh.Description = "Duration (crankshaft degrees)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mg/st";

                    }
                }
                else if (sh.Length == 192)
                {
                    if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        sh.Varname = "Driver wish [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Description = "Throttle  position";
                        sh.Description = "Requested IQ (mg)";
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.Unit = "TPS %";

                    }
                }
                else if (sh.Length == 180)
                {
                    if (sh.Xaxis.Length == 9 && sh.Yaxis.Length == 10)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC1)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int sIQCount = GetMapNameCountForCodeBlock("Start IQ ", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "Start IQ (" + sIQCount.ToString() + ") [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.01;
                            sh.Xaxis.Description = "CT (celcius)";
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.factor.offset = -273.1;
                            sh.Description = "Requested IQ (mg)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "degC";
                        }
                        else if (sh.Xaxis.ID / 256 == 0xC0 && sh.Yaxis.ID / 256 == 0xEC)
                        {
                            // atm boost limit R3 file versions
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Limiters";
                            sh.Varname = "Boost limit map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                            //   sh.factor.factor = 0.01;
                            //sh.Xaxis.factor.factor = 0.01;
                            sh.Yaxis.Description = "Atmospheric pressure (mbar)";
                            sh.Description = "Maximum boost pressure (mbar)";
                            sh.Xaxis.Description = "Engine speed (rpm)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mbar";
                        }
                        else if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                            injDurCount--;
                            //if (injDurCount < 1) injDurCount = 1;
                            sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Yaxis.factor.factor = 0.01;
                            sh.factor.factor = 0.023437;
                            sh.Xaxis.Description = "Engine speed (rpm)";
                            //sh.Yaxis.Description = "Airflow mg/stroke";
                            sh.Yaxis.Description = "Requested Quantity mg/stroke";

                            sh.Description = "Duration (crankshaft degrees)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mg/st";
                        }
                        else if (sh.Xaxis.ID / 256 == 0xC4 && sh.Yaxis.ID / 256 == 0xEA)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                            injDurCount--;
                            //if (injDurCount < 1) injDurCount = 1;
                            sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Yaxis.factor.factor = 0.01;
                            sh.factor.factor = 0.023437;
                            sh.Xaxis.Description = "Engine speed (rpm)";
                            //sh.Yaxis.Description = "Airflow mg/stroke";
                            sh.Yaxis.Description = "Requested Quantity mg/stroke";

                            sh.Description = "Duration (crankshaft degrees)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mg/st";
                        }
                    }
                    else if (sh.Xaxis.Length == 10 && sh.Yaxis.Length == 9)
                    {
                        if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                            injDurCount--;
                            //if (injDurCount < 1) injDurCount = 1;
                            sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Yaxis.factor.factor = 0.01;
                            sh.factor.factor = 0.023437;
                            sh.Xaxis.Description = "Engine speed (rpm)";
                            //sh.Yaxis.Description = "Airflow mg/stroke";
                            sh.Yaxis.Description = "Requested Quantity mg/stroke";

                            sh.Description = "Duration (crankshaft degrees)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mg/st";
                        }
                    }
                }
                else if (sh.Length == 162)
                {
                    if (sh.Xaxis.Length == 9 && sh.Yaxis.Length == 9)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC1)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int sIQCount = GetMapNameCountForCodeBlock("Start IQ ", sh.CodeBlock, newSymbols, false);
                            sh.Varname = "Start IQ (" + sIQCount.ToString() + ") [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.factor.factor = 0.01;
                            sh.Xaxis.Description = "CT (celcius)";
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.factor.offset = -273.1;
                            sh.Description = "Requested IQ (mg)";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "degC";
                        }
                    }
                }
                else if (sh.Length == 160)
                {
                    if (sh.Xaxis.Length == 8 && sh.Yaxis.Length == 10)
                    {
                        if (sh.Xaxis.ID / 256 == 0xC5 && sh.Yaxis.ID / 256 == 0xEC)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            int injDurCount = GetMapNameCountForCodeBlock("Injector duration", sh.CodeBlock, newSymbols, false);
                            injDurCount--;
                            sh.Varname = "Injector duration " + injDurCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Yaxis.factor.factor = 0.01;
                            sh.factor.factor = 0.023437;
                            sh.Xaxis.Description = "Engine speed (rpm)";
                            //sh.Yaxis.Description = "Airflow mg/stroke";
                            sh.Yaxis.Description = "Requested Quantity mg/stroke";

                            sh.Description = "Duration (crankshaft degrees)";
                            sh.Xaxis.Unit = "rpm";
                            sh.Yaxis.Unit = "mg/st";
                        }
                    }
                }
                else if (sh.Length == 150)  // 3L (1.2 TDi, three cylinder VW Lupo) has this
                {
                    if (sh.Xaxis.Length == 3 && sh.Yaxis.Length == 25)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Torque limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Atm. pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";

                    }
                }
                else if (sh.Length == 144)
                {
                    if (sh.Xaxis.Length == 9 && sh.Yaxis.Length == 8)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0)
                        {
                            sh.Category             = "Detected maps";
                            sh.Subcategory          = "Fuel";
                            sh.Varname              = "Fuel volume factor.factor map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Id                   = "zmwMKOR_KF";
                            sh.Description          = "IQ factor.factor per 100K";
                            sh.factor.factor        = 0.002441;
                            sh.Yaxis.Description    = "Engine speed (rpm)";
                            sh.Xaxis.factor.factor  = 0.01;
                            sh.Xaxis.Description    = "IQ (mg/stroke)";
                        }
                    }
                    if (sh.Xaxis.Length == 8 && sh.Yaxis.Length == 9)
                    {
                        if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC1)
                        {
                            sh.Category             = "Detected maps";
                            sh.Subcategory          = "Fuel";
                            int sIQCount            = GetMapNameCountForCodeBlock("Start IQ ", sh.CodeBlock, newSymbols, false);
                            sh.Varname              = "Start IQ (" + sIQCount.ToString() + ") [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Xaxis.Description    = "CT (celcius)";
                            sh.Xaxis.factor.factor  = 0.1;
                            sh.Xaxis.factor.offset  = -273.1;
                            sh.Description          = "Requested IQ (mg)";
                            sh.Yaxis.Description    = "Engine speed (rpm)";
                            sh.factor.factor        = 0.01;
                            sh.Yaxis.Unit           = "rpm";
                            sh.Xaxis.Unit           = "degC";
                        }
                    }
                    if (sh.Xaxis.Length == 3 && sh.Yaxis.Length == 24)
                    {
                        // Tq Lim
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Torque limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Atm. pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";
                    }
                }
                else if (sh.Length == 138)  // R3 (1.4 TDi, three cylinder) has this
                {
                    if (sh.Xaxis.Length == 3 && sh.Yaxis.Length == 23)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Torque limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Atm. pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";

                    }
                }
                else if (sh.Length == 132)  // R3 (1.4 TDi, three cylinder) has this
                {
                    if (sh.Xaxis.Length == 3 && sh.Yaxis.Length == 22)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Torque limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Atm. pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";

                    }
                }
                else if (sh.Length == 128)
                {
                    if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC1)
                    {
                        // check for valid axis data on temp data
                        if (IsValidTemperatureAxis(allBytes, sh, AxisIdent.Y_Axis))
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Limiters";
                            int maflimTempCount = GetMapNameCountForCodeBlock("MAF factor.factor by temperature", sh.CodeBlock, newSymbols, false);
                            maflimTempCount--;
                            sh.Varname = "MAF factor.factor by temperature " + maflimTempCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Description = "Limit";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Xaxis.Description = "Intake air temperature"; //IAT, ECT or Fuel temp?
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.factor.offset = -273.1;
                            sh.factor.factor = 0.01;
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "°C";
                        }

                    }
                    else if (sh.Xaxis.ID / 256 == 0xEA && sh.Yaxis.ID / 256 == 0xC1)
                    {
                        // check for valid axis data on temp data
                        if (IsValidTemperatureAxis(allBytes, sh, AxisIdent.Y_Axis))
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Limiters";
                            int maflimTempCount = GetMapNameCountForCodeBlock("MAF factor.factor by temperature", sh.CodeBlock, newSymbols, false);
                            maflimTempCount--;
                            sh.Varname = "MAF factor.factor by temperature " + maflimTempCount.ToString("D2") + " [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Description = "Limit";
                            sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Xaxis.Description = "Intake air temperature"; //IAT, ECT or Fuel temp?
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.factor.offset = -273.1;
                            sh.factor.factor = 0.01;
                            sh.Yaxis.Unit = "rpm";
                            sh.Xaxis.Unit = "°C";
                        }

                    }
                    else if (sh.Xaxis.ID / 256 == 0xEC && sh.Yaxis.ID / 256 == 0xC0) // EXPERIMENTAL
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Expected fuel temperature [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Id = "zmwMKBT_KF";
                        sh.factor.factor = 0.1;
                        sh.factor.offset = -273.1;
                        sh.Yaxis.Description = "Engine speed (rpm)";
                        sh.Yaxis.Unit = "rpm";
                        sh.Xaxis.factor.factor = 0.01;
                        sh.Xaxis.Unit = "mg/st";
                        sh.Xaxis.Description = "IQ (mg/stroke)";
                        sh.Description = "Fuel temperature °C";


                    }

                }
                else if (sh.Length == 126)
                {
                    if (sh.Xaxis.Length == 3 && sh.Yaxis.Length == 21)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Torque limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Atm. pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";
                        sh.factor.factor = 0.01;
                    }
                }
                else if (sh.Length == 120)
                {
                    if (sh.Xaxis.Length == 3 && sh.Yaxis.Length == 20)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        sh.Varname = "Torque limiter [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        sh.Description = "Maximum IQ (mg)";
                        sh.Yaxis.Description = "Atm. pressure (mbar)";
                        sh.Xaxis.Description = "Engine speed (rpm)";
                        sh.factor.factor = 0.01;
                        sh.Xaxis.Unit = "rpm";
                        sh.Yaxis.Unit = "mbar";

                    }
                }
                else if (sh.Length == 64)
                {
                    if (sh.Xaxis.Length == 32 && sh.Yaxis.Length == 1)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Misc";
                        sh.Varname = "MAF linearization [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                    }
                }
                else if (sh.Length == 60)
                {
                    if (sh.Yaxis.Length == 6 && sh.Xaxis.Length == 5)
                    {
                        if (sh.Yaxis.ID == 0xC1A2)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            sh.Varname = "EGR temperature map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Xaxis.Description = "Temperature"; //IAT, ECT or Fuel temp?
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.factor.offset = -273.1;
                            sh.Description = "Mass airflow factor.factor";
                        }
                    }
                }
                else if (sh.Length >= 18 && sh.Length <= 70)
                {
                    if (sh.Xaxis.ID / 16 == 0xC1A && sh.Yaxis.ID / 16 == 0xEC3)
                    {
                        sh.Category = "Detected maps";
                        sh.Subcategory = "Limiters";
                        //Temp after intercooler
                        sh.Yaxis.Description = "Temperature";
                        sh.Xaxis.Description = "Engine speed (rpm)"; //IAT, ECT or Fuel temp?
                        sh.Yaxis.factor.factor = 0.1;
                        sh.Yaxis.factor.offset = -273.1;
                        sh.Description = "%";
                        sh.factor.factor = 0.01;
                        sh.Varname = "IQ by air intake temp[" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                    }
                }
                else if (sh.Length == 20)
                {
                    if (sh.Yaxis.Length == 5 && sh.Xaxis.Length == 2)
                    {
                        //if (sh.Yaxis.ID == 0xC1A2)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            //sh.Yaxis.Description = "Engine speed (rpm)";
                            sh.Yaxis.Description = "Air pressure";
                            sh.Xaxis.Description = "Temperature"; //IAT, ECT or Fuel temp?
                            sh.Xaxis.factor.factor = 0.1;
                            sh.Xaxis.factor.offset = -273.1;
                            sh.Description = "Time (sec)";
                            sh.factor.factor = 0.01;
                            sh.Varname = "Pre-glow map [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        }
                    }
                }
                else if (sh.Length == 12)
                {
                    if (sh.Xaxis.Length == 6 && sh.Yaxis.Length == 1)
                    {
                        if ((sh.Xaxis.ID & 0xFFF0) == 0xECB0)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Fuel";
                            sh.Varname = "Selector for injector duration [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            // soi values as axis!!
                            sh.Yaxis.factor.factor = -0.023437;
                            sh.Yaxis.factor.offset = 78;
                            sh.factor.factor = 0.00390625;
                            sh.Description = "Map index";

                            sh.Yaxis.Unit = "SOI";
                        }
                    }
                }
                else if (sh.Length == 8)
                {
                    /*if (sh.Xaxis.ID / 256 == 0xC1) // idle RPM
                    {
                        if (IsValidTemperatureAxis(allBytes, sh, MapViewerEx.AxisIdent.X_Axis))
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            int lmCount = GetMapNameCountForCodeBlock("Idle RPM", sh.CodeBlock, newSymbols, false);

                            sh.Varname = "Idle RPM (" + lmCount.ToString() + ") [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                            sh.Yaxis.Description = "Coolant temperature";
                            sh.Yaxis.factor.factor = 0.1;
                            sh.Yaxis.factor.offset = -273.1;
                            sh.Description = "Target engine speed";
                            sh.Yaxis.Unit = "°C";
                        }

                    }*/
                }
                else if (sh.Length == 4)
                {
                    if (sh.Xaxis.Length == 2 && sh.Yaxis.Length == 1)
                    {
                        if (sh.Xaxis.ID == 0xEBA2 || sh.Xaxis.ID == 0xEBA4 || sh.Xaxis.ID == 0xE9BC)
                        {
                            sh.Category = "Detected maps";
                            sh.Subcategory = "Misc";
                            sh.Varname = "MAP linearization [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                        }
                        else if (sh.Xaxis.ID / 256 == 0xC1) // idle RPM
                        {
                            if (IsValidTemperatureAxis(allBytes, sh, AxisIdent.X_Axis))
                            {
                                sh.Category = "Detected maps";
                                sh.Subcategory = "Misc";
                                int lmCount = GetMapNameCountForCodeBlock("Idle RPM", sh.CodeBlock, newSymbols, false);

                                sh.Varname = "Idle RPM (" + lmCount.ToString() + ") [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";
                                sh.Yaxis.Description = "Coolant temperature";
                                sh.Yaxis.factor.factor = 0.1;
                                sh.Yaxis.factor.offset = -273.1;
                                sh.Description = "Target engine speed";
                                sh.Yaxis.Unit = "°C";
                            }

                        }
                    }
                }
                if (sh.Xaxis.ID == 0xDA6C && sh.Yaxis.ID == 0xDA6A)
                {
                    sh.Category = "Detected maps";
                    sh.Xaxis.factor.factor = 0.1;
                    sh.Xaxis.factor.offset = -273.1;
                    sh.Xaxis.Unit = "°C";
                    sh.Subcategory = "Limiters";
                    sh.Varname = "Boost factor.factor by temperature [" + DetermineNumberByFlashBank(sh.StartAddress, newCodeBlocks) + "]";// " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.ID.ToString("X4") + " " + sh.Yaxis.ID.ToString("X4");
                    sh.Xaxis.Description = "IAT (celcius)";
                    sh.Yaxis.Description = "Requested boost";
                    sh.Description = "Boost limit (mbar)";
                    sh.Yaxis.Unit = "mbar";
                }
            }
        }

        private bool MapSelectorIndexEmpty(SymbolHelper sh)
        {
            bool retval = true;
            if (sh.MapSelector != null)
            {
                foreach (int iTest in sh.MapSelector.MapIndexes)
                {
                    if (iTest != 0) retval = false;
                }
            }
            return retval;
        }

        private int GetMaxAxisValue(byte[] allBytes, SymbolHelper sh, ECUsuite.ECU.Base. AxisIdent axisIdent)
        {
            int retval = 0;
            if (axisIdent == AxisIdent.X_Axis)
            {
                //read x axis values
                int offset = sh.Xaxis.StartAddress;
                for (int i = 0; i < sh.Xaxis.Length; i++)
                {
                    int val = Convert.ToInt32(allBytes[offset]) + Convert.ToInt32(allBytes[offset + 1]) * 256;
                    if (val > retval) retval = val;
                    offset += 2;
                }
            }
            else if (axisIdent == AxisIdent.Y_Axis)
            {
                //read x axis values
                int offset = sh.Yaxis.StartAddress;
                for (int i = 0; i < sh.Yaxis.Length; i++)
                {
                    int val = Convert.ToInt32(allBytes[offset]) + Convert.ToInt32(allBytes[offset + 1]) * 256;
                    if (val > retval) retval = val;
                    offset += 2;
                }
            }
            return retval;
        }

        private bool IsValidTemperatureAxis(byte[] allBytes, SymbolHelper sh, AxisIdent axisIdent)
        {
            bool retval = true;
            if (axisIdent ==  AxisIdent.X_Axis)
            {
                //read x axis values
                int offset = sh.Xaxis.StartAddress;
                for (int i = 0; i < sh.Xaxis.Length; i++)
                {
                    int val = Convert.ToInt32(allBytes[offset]) + Convert.ToInt32(allBytes[offset + 1]) * 256;
                    double tempVal = Convert.ToDouble(val) * 0.1 - 273.1;
                    if (tempVal < -80 || tempVal > 200) retval = false;
                    offset += 2;
                }
            }
            else if (axisIdent == AxisIdent.Y_Axis)
            {
                //read x axis values
                int offset = sh.Yaxis.StartAddress;
                for (int i = 0; i < sh.Yaxis.Length; i++)
                {
                    int val = Convert.ToInt32(allBytes[offset]) + Convert.ToInt32(allBytes[offset + 1]) * 256;
                    double tempVal = Convert.ToDouble(val) * 0.1 - 273.1;
                    if (tempVal < -80 || tempVal > 200) retval = false;
                    offset += 2;
                }
            }
            return retval;
        }

        private double GetTemperatureDurRange(int index)
        {
            double retval = 0;
            // 
            return retval;
        }

        //SOI is selected on coolant temperature!
        private double GetTemperatureSOIRange(MapSelector sh, int index)
        {
            double retval = index;
            if (sh.MapData != null)
            {
                if (sh.MapData.Length > index)
                {
                    retval = Convert.ToDouble(sh.MapData.GetValue(index)) * 0.1 - 273.1;
                }

            }
            return Math.Round(retval, 0);
        }

        private bool MapContainsNegativeValues(byte[] allBytes, SymbolHelper sh)
        {
            for (int i = 0; i < sh.Length; i += 2)
            {
                int currval = Convert.ToInt32(allBytes[sh.StartAddress + i + 1]) * 256 + Convert.ToInt32(allBytes[sh.StartAddress + i]);
                if (currval > 0xF000) return true;
            }
            return false;

        }

        private int GetMapNameCountForCodeBlock(string varName, int codeBlock, SymbolCollection newSymbols, bool debug)
        {
            int count = 0;
            if (debug) Console.WriteLine("Check " + varName + " " + codeBlock);

            foreach (SymbolHelper sh in newSymbols)
            {
                if (debug)
                {
                    if (!sh.Varname.StartsWith("2D") && !sh.Varname.StartsWith("3D"))
                    {
                        Console.WriteLine(sh.Varname + " " + sh.CodeBlock);
                    }
                }
                if (sh.Varname.StartsWith(varName) && sh.CodeBlock == codeBlock)
                {

                    if (debug) Console.WriteLine("Found " + sh.Varname + " " + sh.CodeBlock);

                    count++;
                }
            }
            count++;
            return count;
        }

        private bool AddToSymbolCollection(SymbolCollection newSymbols, SymbolHelper newSymbol, List<CodeBlock> newCodeBlocks)
        {
            if (newSymbol.Length >= 800) return false;
            foreach (SymbolHelper sh in newSymbols)
            {
                if (sh.StartAddress == newSymbol.StartAddress)
                {
                    //   Console.WriteLine("Already in collection: " + sh.StartAddress.ToString("X8"));
                    return false;
                }
                // not allowed to overlap
                /*else if (newSymbol.StartAddress > sh.StartAddress && newSymbol.StartAddress < (sh.StartAddress + sh.Length))
                {
                    Console.WriteLine("Overlapping map: " + sh.StartAddress.ToString("X8") + " " + sh.Xaxis.Length.ToString() + " x " + sh.Yaxis.Length.ToString());
                    Console.WriteLine("Overlapping new: " + newSymbol.StartAddress.ToString("X8") + " " + newSymbol.Xaxis.Length.ToString() + " x " + newSymbol.Yaxis.Length.ToString());
                    return false;
                }*/
            }
            newSymbols.Add(newSymbol);
            newSymbol.CodeBlock = DetermineCodeBlockByByAddress(newSymbol.StartAddress, newCodeBlocks);
            return true;
        }

        private bool isValidLength(int length, int id)
        {
            int idstrip = id / 256;
            if ((idstrip & 0xF0) == 0xE0)
            //if (idstrip == 0xEB /*|| idstrip == 0xDE*/)
            {
                if (length > 0 && length <= 32) return true;
            }
            else
            {
                if (length > 0 && length < 32) return true;
            }
            //if (length <= 64) Console.WriteLine("seen id " + id.ToString("X4") + " with len " + length.ToString());

            return false;
        }

        private bool isAxisID(int id)
        {
            int idstrip = id / 256;
            if (idstrip == 0xDB) return true;
            if (idstrip == 0xC0 || idstrip == 0xC1 || idstrip == 0xC2 || idstrip == 0xC4 || idstrip == 0xC5) return true;
            if (idstrip == 0xE0 || idstrip == 0xE4 || idstrip == 0xE5 || idstrip == 0xE9 || idstrip == 0xEA || idstrip == 0xEB || idstrip == 0xEC) return true;
            if (idstrip == 0xDA /*|| idstrip == 0xDC */|| idstrip == 0xDD || idstrip == 0xDE) return true;
            if (idstrip == 0xF9 || idstrip == 0xFE) return true;
            if (idstrip == 0xE8) return true;
            //if (idstrip == 0xD7 || idstrip == 0xE6) return true;
            // if (idstrip == 0xD5) return true;
            return false;
        }

        // we need to check AHEAD for selector maps
        // if these are present we may be facing a complex map structure
        // which we need to handle in a special way (selectors always have data like 00 01 00 02 00 03 00 04 etc)
        private bool CheckMap(int t, byte[] allBytes, SymbolCollection newSymbols, List<CodeBlock> newCodeBlocks, out int len2Skip)
        {
            bool mapFound = false;
            bool retval = false;
            bool _dontGenMaps = false;
            len2Skip = 0;
            List<MapSelector> mapSelectors = new List<MapSelector>();
            if (t < allBytes.Length - 0x100)
            {
                /*if (t > 0x58000 && t < 0x60000)
                {
                    Console.WriteLine("Checkmap: " + t.ToString("X8"));
                }*/
                if (CheckAxisCount(t, allBytes, out mapSelectors) > 3)
                {
                    // check for selectors as well, and count them in the process
                    //Console.WriteLine("Offset " + t.ToString("X8") + " has more than 3 consecutive axis");
                    /*foreach (MapSelector ms in mapSelectors)
                    {
                        Console.WriteLine("selector: " + ms.StartAddress.ToString("X8") + " " + ms.MapLength.ToString() + " " + ms.NumRepeats.ToString());
                    }*/
                    _dontGenMaps = true;

                }

                int xaxisid = Convert.ToInt32(allBytes[t + 1]) * 256 + Convert.ToInt32(allBytes[t]);

                if (isAxisID(xaxisid))
                {
                    int xaxislen = Convert.ToInt32(allBytes[t + 3]) * 256 + Convert.ToInt32(allBytes[t + 2]);
                    // Console.WriteLine("Valid XID: " + xaxisid.ToString("X4") + " @" + t.ToString("X8") + " len: " + xaxislen.ToString("X2"));
                    if (isValidLength(xaxislen, xaxisid))
                    {
                        //Console.WriteLine("Valid XID: " + xaxisid.ToString("X4") + " @" + t.ToString("X8") + " len: " + xaxislen.ToString("X2"));
                        // misschien is er nog een as
                        int yaxisid = Convert.ToInt32(allBytes[t + 5 + xaxislen * 2]) * 256 + Convert.ToInt32(allBytes[t + 4 + xaxislen * 2]);
                        int yaxislen = Convert.ToInt32(allBytes[t + 7 + xaxislen * 2]) * 256 + Convert.ToInt32(allBytes[t + 6 + xaxislen * 2]);
                        if (isAxisID(yaxisid) && isValidLength(yaxislen, yaxisid))
                        {
                            // 3d map

                            int zaxisid = Convert.ToInt32(allBytes[t + 9 + xaxislen * 2 + yaxislen * 2]) * 256 + Convert.ToInt32(allBytes[t + 8 + xaxislen * 2 + yaxislen * 2]);
                            //Console.WriteLine("Valid YID: " + yaxisid.ToString("X4") + " @" + t.ToString("X8") + " len: " + yaxislen.ToString("X2"));


                            //Console.WriteLine(t.ToString("X8") + " XID: " + xaxisid.ToString("X4") + " XLEN: " + xaxislen.ToString("X2") + " YID: " + yaxisid.ToString("X4") + " YLEN: " + yaxislen.ToString("X2"));
                            SymbolHelper newSymbol = new SymbolHelper();
                            newSymbol.Xaxis.Length = xaxislen;
                            newSymbol.Yaxis.Length = yaxislen;
                            newSymbol.Xaxis.ID = xaxisid;
                            newSymbol.Yaxis.ID = yaxisid;
                            newSymbol.Xaxis.StartAddress = t + 4;
                            newSymbol.Yaxis.StartAddress = t + 8 + xaxislen * 2;

                            newSymbol.Length = xaxislen * yaxislen * 2;
                            newSymbol.StartAddress = t + 8 + xaxislen * 2 + yaxislen * 2;
                            if (isAxisID(zaxisid))
                            {
                                int zaxislen = Convert.ToInt32(allBytes[t + 11 + xaxislen * 2 + yaxislen * 2]) * 256 + Convert.ToInt32(allBytes[t + 10 + xaxislen * 2 + yaxislen * 2]);

                                int zaxisaddress = t + 12 + xaxislen * 2 + yaxislen * 2;

                                if (isValidLength(zaxislen, zaxisid))
                                {
                                    //   newSymbol.StartAddress += 0x10; // dan altijd 16 erbij
                                    int len2skip = 4 + zaxislen * 2;
                                    if (len2skip < 16) len2skip = 16; // at least 16 bytes
                                    newSymbol.StartAddress += len2skip;
                                    len2Skip += xaxislen * 2 + yaxislen * 2 + zaxislen * 2;

                                    if (!_dontGenMaps)
                                    {
                                        // this has something to do with repeating several times with the same axis set

                                        Console.WriteLine("Added " + len2skip.ToString() + " because of z axis " + newSymbol.StartAddress.ToString("X8"));


                                        // maybe there are multiple maps between the end of the map and the start of the next axis
                                        int nextMapAddress = findNextMap(allBytes, (int)(newSymbol.StartAddress + newSymbol.Length), newSymbol.Length * 10);
                                        if (nextMapAddress > 0)
                                        {
                                            // is it divisable by the maplength

                                            if ((nextMapAddress - newSymbol.StartAddress) % newSymbol.Length == 0)
                                            {

                                                int numberOfrepeats = (int)(nextMapAddress - newSymbol.StartAddress) / newSymbol.Length;
                                                numberOfrepeats = zaxislen;
                                                if (numberOfrepeats > 1)
                                                {
                                                    MapSelector ms = new MapSelector();
                                                    ms.NumRepeats = numberOfrepeats;
                                                    ms.MapLength = newSymbol.Length;
                                                    ms.StartAddress = zaxisaddress;
                                                    ms.XAxisAddress = newSymbol.Xaxis.StartAddress;
                                                    ms.YAxisAddress = newSymbol.Yaxis.StartAddress;
                                                    ms.XAxisLen = newSymbol.Xaxis.Length;
                                                    ms.YAxisLen = newSymbol.Yaxis.Length;
                                                    ms.MapData = new int[zaxislen];
                                                    int boffset = 0;
                                                    for (int ia = 0; ia < zaxislen; ia++)
                                                    {
                                                        int axisValue = Convert.ToInt32(allBytes[zaxisaddress + boffset]) + Convert.ToInt32(allBytes[zaxisaddress + boffset + 1]) * 256;
                                                        ms.MapData.SetValue(axisValue, ia);
                                                        boffset += 2;
                                                    }

                                                    ms.MapIndexes = new int[zaxislen];
                                                    for (int ia = 0; ia < zaxislen; ia++)
                                                    {
                                                        int axisValue = Convert.ToInt32(allBytes[zaxisaddress + boffset]) + Convert.ToInt32(allBytes[zaxisaddress + boffset + 1]) * 256;
                                                        ms.MapIndexes.SetValue(axisValue, ia);
                                                        boffset += 2;
                                                    }

                                                    // numberOfrepeats--;
                                                    //int idx = 0;

                                                    for (int maprepeat = 0; maprepeat < numberOfrepeats; maprepeat++)
                                                    {
                                                        // idx ++;
                                                        SymbolHelper newGenSym = new SymbolHelper();
                                                        newGenSym.Xaxis.Length = newSymbol.Xaxis.Length;
                                                        newGenSym.Yaxis.Length = newSymbol.Yaxis.Length;
                                                        newGenSym.Xaxis.ID = newSymbol.Xaxis.ID;
                                                        newGenSym.Yaxis.ID = newSymbol.Yaxis.ID;
                                                        newGenSym.Xaxis.StartAddress = newSymbol.Xaxis.StartAddress;
                                                        newGenSym.Yaxis.StartAddress = newSymbol.Yaxis.StartAddress;
                                                        newGenSym.StartAddress = newSymbol.StartAddress + maprepeat * newSymbol.Length;
                                                        newGenSym.Length = newSymbol.Length;
                                                        newGenSym.Varname = "3D GEN " + newGenSym.StartAddress.ToString("X8") + " " + xaxisid.ToString("X4") + " " + yaxisid.ToString("X4");
                                                        newGenSym.MapSelector = ms;
                                                        // attach a mapselector to these maps
                                                        // only add it if the map is not empty
                                                        // otherwise we will cause confusion among users
                                                        if (maprepeat > 0)
                                                        {
                                                            try
                                                            {
                                                                if (ms.MapIndexes[maprepeat] > 0)
                                                                {
                                                                    retval = AddToSymbolCollection(newSymbols, newGenSym, newCodeBlocks);
                                                                    if (retval)
                                                                    {
                                                                        mapFound = true;
                                                                        //GUIDO len2Skip += newGenSym.Length;
                                                                        //t += (xaxislen * 2) + (yaxislen * 2) + newGenSym.Length;
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception)
                                                            {
                                                            }
                                                        }
                                                        else
                                                        {
                                                            retval = AddToSymbolCollection(newSymbols, newGenSym, newCodeBlocks);
                                                            if (retval)
                                                            {
                                                                mapFound = true;
                                                                //GUIDO len2Skip += (xaxislen * 2) + (yaxislen * 2) + newGenSym.Length;
                                                                //t += (xaxislen * 2) + (yaxislen * 2) + newGenSym.Length;
                                                            }
                                                        }
                                                    }
                                                }
                                                //Console.WriteLine("Indeed!");
                                                // the first one will be added anyway.. add the second to the last

                                            }

                                        }
                                    }
                                    else
                                    {

                                        int maxisid = Convert.ToInt32(allBytes[t + 13 + xaxislen * 2 + yaxislen * 2 + zaxislen * 2]) * 256 + Convert.ToInt32(allBytes[t + 12 + xaxislen * 2 + yaxislen * 2 + zaxislen * 2]);
                                        int maxislen = Convert.ToInt32(allBytes[t + 15 + xaxislen * 2 + yaxislen * 2 + zaxislen * 2]) * 256 + Convert.ToInt32(allBytes[t + 14 + xaxislen * 2 + yaxislen * 2 + zaxislen * 2]);
                                        //maxislen *= 2;

                                        int maxisaddress = t + 16 + xaxislen * 2 + yaxislen * 2;

                                        if (isAxisID(maxisid))
                                        {
                                            newSymbol.StartAddress += maxislen * 2 + 4;
                                        }
                                        // special situation, handle selectors
                                        //Console.WriteLine("Map start address = " + newSymbol.StartAddress.ToString("X8"));
                                        long lastFlashAddress = newSymbol.StartAddress;
                                        foreach (MapSelector ms in mapSelectors)
                                        {

                                            // check the memory size between the start of the map and the 
                                            // start of the map selector
                                            long memsize = ms.StartAddress - lastFlashAddress;
                                            memsize /= 2; // in words
                                            if (ms.NumRepeats > 0)
                                            {
                                                int mapsize = Convert.ToInt32(memsize) / ms.NumRepeats;
                                                if (xaxislen * yaxislen == mapsize)
                                                {
                                                    //Console.WriteLine("selector: " + ms.StartAddress.ToString("X8") + " " + ms.MapLength.ToString() + " " + ms.NumRepeats.ToString());
                                                    //Console.WriteLine("memsize = " + memsize.ToString() + " mapsize " + mapsize.ToString());
                                                    //Console.WriteLine("starting at address: " + lastFlashAddress.ToString("X8"));
                                                    // first axis set
                                                    //len2Skip += (xaxislen * 2) + (yaxislen * 2);
                                                    for (int i = 0; i < ms.NumRepeats; i++)
                                                    {
                                                        SymbolHelper shGen2 = new SymbolHelper();
                                                        shGen2.MapSelector = ms;
                                                        shGen2.Xaxis.Length = newSymbol.Xaxis.Length;
                                                        shGen2.Yaxis.Length = newSymbol.Yaxis.Length;
                                                        shGen2.Xaxis.ID = newSymbol.Xaxis.ID;
                                                        shGen2.Yaxis.ID = newSymbol.Yaxis.ID;
                                                        shGen2.Xaxis.StartAddress = newSymbol.Xaxis.StartAddress;
                                                        shGen2.Yaxis.StartAddress = newSymbol.Yaxis.StartAddress;
                                                        shGen2.Length = mapsize * 2;
                                                        //shGen2.Category = "Generated";
                                                        long address = lastFlashAddress;
                                                        shGen2.StartAddress = (int)address;
                                                        //shGen2.factor.factor = 0.023437; // TEST
                                                        //shGen2.Varname = "Generated* " + shGen2.StartAddress.ToString("X8") + " " + ms.StartAddress.ToString("X8") + " " + ms.NumRepeats.ToString() + " " + i.ToString();
                                                        shGen2.Varname = "3D " + shGen2.StartAddress.ToString("X8") + " " + shGen2.Xaxis.ID.ToString("X4") + " " + shGen2.Yaxis.ID.ToString("X4");
                                                        //if (i < ms.NumRepeats - 1)
                                                        {
                                                            retval = AddToSymbolCollection(newSymbols, shGen2, newCodeBlocks);
                                                            if (retval)
                                                            {
                                                                mapFound = true;
                                                                //GUIDO len2Skip += shGen2.Length;
                                                                //t += (xaxislen * 2) + (yaxislen * 2) + shGen2.Length;
                                                            }
                                                        }
                                                        lastFlashAddress = address + mapsize * 2;
                                                        // Console.WriteLine("Set last address to " + lastFlashAddress.ToString("X8"));
                                                    }
                                                    lastFlashAddress += ms.NumRepeats * 4 + 4;
                                                }
                                                else if (zaxislen * maxislen == mapsize)
                                                {
                                                    // second axis set
                                                    // len2Skip += (xaxislen * 2) + (yaxislen * 2);
                                                    for (int i = 0; i < ms.NumRepeats; i++)
                                                    {
                                                        SymbolHelper shGen2 = new SymbolHelper();
                                                        shGen2.MapSelector = ms;
                                                        shGen2.Xaxis.Length = maxislen;
                                                        shGen2.Yaxis.Length = zaxislen;
                                                        shGen2.Xaxis.ID = maxisid;
                                                        shGen2.Yaxis.ID = zaxisid;
                                                        shGen2.Xaxis.StartAddress = maxisaddress;
                                                        shGen2.Yaxis.StartAddress = zaxisaddress;
                                                        shGen2.Length = mapsize * 2;
                                                        //shGen2.Category = "Generated";
                                                        long address = lastFlashAddress;
                                                        shGen2.StartAddress = (int)address;
                                                        //shGen2.Varname = "Generated** " + shGen2.StartAddress.ToString("X8");
                                                        shGen2.Varname = "3D " + shGen2.StartAddress.ToString("X8") + " " + shGen2.Xaxis.ID.ToString("X4") + " " + shGen2.Yaxis.ID.ToString("X4");

                                                        //if (i < ms.NumRepeats - 1)
                                                        {
                                                            retval = AddToSymbolCollection(newSymbols, shGen2, newCodeBlocks);
                                                            if (retval)
                                                            {
                                                                mapFound = true;
                                                                //GUIDO len2Skip += shGen2.Length;
                                                                //t += (xaxislen * 2) + (yaxislen * 2) + shGen2.Length;
                                                            }
                                                        }
                                                        lastFlashAddress = address + mapsize * 2;
                                                        //Console.WriteLine("Set last address 2 to " + lastFlashAddress.ToString("X8"));
                                                    }
                                                    lastFlashAddress += ms.NumRepeats * 4 + 4;
                                                }
                                            }
                                            //if(ms.NumRepeats

                                        }
                                    }
                                }
                            }

                            newSymbol.Varname = "3D " + newSymbol.StartAddress.ToString("X8") + " " + xaxisid.ToString("X4") + " " + yaxisid.ToString("X4");
                            //Console.WriteLine(newSymbol.Varname + " " + newSymbol.Length.ToString() + " " + newSymbol.Xaxis.Length.ToString() + "x" + newSymbol.Yaxis.Length.ToString());
                            retval = AddToSymbolCollection(newSymbols, newSymbol, newCodeBlocks);
                            if (retval)
                            {
                                mapFound = true;
                                //GUIDO len2Skip += (xaxislen * 2) + (yaxislen * 2) + newSymbol.Length;
                                //t += (xaxislen * 2) + (yaxislen * 2) + newSymbol.Length;
                            }

                        }
                        else
                        {
                            if (yaxisid > 0xC000 && yaxisid < 0xF000 && yaxislen <= 32) Console.WriteLine("Unknown map id: " + yaxisid.ToString("X4") + " len " + yaxislen.ToString("X4") + " at address " + t.ToString("X8"));
                            SymbolHelper newSymbol = new SymbolHelper();
                            newSymbol.Xaxis.Length = xaxislen;
                            newSymbol.Xaxis.ID = xaxisid;
                            newSymbol.Xaxis.StartAddress = t + 4;
                            newSymbol.Length = xaxislen * 2;
                            newSymbol.StartAddress = t + 4 + xaxislen * 2;
                            newSymbol.Varname = "2D " + newSymbol.StartAddress.ToString("X8") + " " + xaxisid.ToString("X4");
                            //newSymbols.Add(newSymbol);
                            newSymbol.CodeBlock = DetermineCodeBlockByByAddress(newSymbol.StartAddress, newCodeBlocks);
                            retval = AddToSymbolCollection(newSymbols, newSymbol, newCodeBlocks);
                            if (retval)
                            {
                                mapFound = true;
                                //GUIDO len2Skip += (xaxislen * 2);
                                //t += (xaxislen * 2);
                            }
                            // 2d map
                        }
                    }

                }
            }
            return mapFound;
        }

        private bool MapIsEmpty(byte[] allBytes, SymbolHelper sh)
        {
            for (int i = 0; i < sh.Length; i += 2)
            {
                int currval = Convert.ToInt32(allBytes[sh.StartAddress + i + 1]) * 256 + Convert.ToInt32(allBytes[sh.StartAddress + i]);
                if (currval != 0) return false;
            }
            return true;
        }

        private int findNextMap(byte[] allBytes, int index, int maxBytesToSearch)
        {
            int retval = 0;
            for (int i = index; i < index + maxBytesToSearch; i += 2)
            {
                int xaxisid = Convert.ToInt32(allBytes[i + 1]) * 256 + Convert.ToInt32(allBytes[i]);
                if (isAxisID(xaxisid))
                {
                    int xaxislen = Convert.ToInt32(allBytes[i + 3]) * 256 + Convert.ToInt32(allBytes[i + 2]);
                    if (isValidLength(xaxislen, xaxisid))
                    {
                        return i;
                    }
                }
            }
            return retval;
        }

        private int CheckAxisCount(int offset, byte[] allBytes, out List<MapSelector> mapSelectors)
        {
            int axisCount = 0;
            /*if (offset == 0x58504)
            {
                Console.WriteLine("58504");
            }*/
            mapSelectors = new List<MapSelector>();
            bool axisFound = true;
            int t = offset;
            while (axisFound)
            {
                axisFound = false;
                int axisid = Convert.ToInt32(allBytes[t + 1]) * 256 + Convert.ToInt32(allBytes[t]);
                if (isAxisID(axisid))
                {
                    int axislen = Convert.ToInt32(allBytes[t + 3]) * 256 + Convert.ToInt32(allBytes[t + 2]);
                    if (axislen > 0 && axislen < 32)
                    {
                        axisCount++;
                        axisFound = true;
                        t += 4 + axislen * 2;
                    }

                }
            }
            // search from offset 't' for selectors
            // maximum searchrange = 0x1000
            int BytesToSearch = 5120 + 16;
            if (axisCount > 3)
            {
                while (BytesToSearch > 0)
                {
                    int axisid = Convert.ToInt32(allBytes[t + 1]) * 256 + Convert.ToInt32(allBytes[t]);
                    if (isAxisID(axisid))
                    {
                        //Console.WriteLine("Checking address: " + t.ToString("X8"));
                        int axislen = Convert.ToInt32(allBytes[t + 3]) * 256 + Convert.ToInt32(allBytes[t + 2]);
                        if (axislen <= 10) // more is not valid for selectors
                        {
                            // read & verify data (00 00 00 01 00 02 00 03 etc)
                            bool selectorValid = true;
                            int num = 0;
                            uint prevSelector = 0;
                            for (int i = 0; i < axislen * 2; i += 2)
                            {
                                uint selValue = Convert.ToUInt32(allBytes[t + 4 + axislen * 2 + i]) + Convert.ToUInt32(allBytes[t + 4 + axislen * 2 + 1 + i]);
                                //Console.WriteLine("Selval: " + selValue.ToString() + " num: " + num.ToString());
                                /*if (axislen < 3)
                                {
                                    selectorValid = false;
                                    break;
                                }*/
                                if (allBytes[t + 4 + axislen * 2 + i] != 0)
                                {
                                    if (allBytes[t + 4 + axislen * 2 + i] != 0x40) selectorValid = false;
                                    break;
                                }
                                if (allBytes[t + 4 + axislen * 2 + 1 + i] > 9)
                                {
                                    selectorValid = false;
                                    break;
                                }
                                if (prevSelector > selValue)
                                {
                                    selectorValid = false;
                                    break;
                                }
                                prevSelector = selValue;
                                /*if (num != selValue)
                                {
                                    // not a valid selector
                                    selectorValid = false;
                                    break;
                                }*/
                                num++;
                            }
                            if (selectorValid)
                            {
                                // create a new selector
                                //Console.WriteLine("Selector valid " + t.ToString("X8"));
                                MapSelector newSel = new MapSelector();
                                newSel.NumRepeats = axislen;
                                newSel.StartAddress = t;

                                // read the data into the mapselector
                                newSel.MapData = new int[axislen];
                                int boffset = 0;
                                for (int ia = 0; ia < axislen; ia++)
                                {
                                    int axisValue = Convert.ToInt32(allBytes[newSel.StartAddress + 4 + boffset]) + Convert.ToInt32(allBytes[newSel.StartAddress + 4 + boffset + 1]) * 256;
                                    newSel.MapData.SetValue(axisValue, ia);
                                    boffset += 2;
                                }
                                mapSelectors.Add(newSel);
                                if (mapSelectors.Count > 5) break;

                                BytesToSearch = 5120 + 16;
                            }
                        }
                    }
                    t += 2;
                    BytesToSearch -= 2;
                }
            }
            return axisCount;
        }


        #region Single Value Boost limiter
        public void FindSVBL(byte[] allBytes, SymbolCollection newSymbols, List<CodeBlock> newCodeBlocks)
        {
            /*
            if (!FindSVBLSequenceOne(allBytes, newSymbols, newCodeBlocks))
            {
                FindSVBLSequenceTwo(allBytes, newSymbols, newCodeBlocks);
            }
            */
        }

        private bool FindSVBLSequenceOne(byte[] allBytes, SymbolCollection newSymbols, List<CodeBlock> newCodeBlocks)
        {
            bool found = true;
            bool SVBLFound = false;
            int offset = 0;

            while (found)
            {
                //int SVBLAddress = tools.findSequence(allBytes, offset, new byte[10] { 0xDF, 0x7A, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0xDF, 0x7A }, new byte[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
                int SVBLAddress = tools.findSequence(allBytes, offset, new byte[16] { 0xD2, 0x00, 0xFC, 0x03, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0xFF, 0xFF, 0xFF, 0xC3, 0x00, 0x00 }, new byte[16] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1 });

                if (SVBLAddress > 0)
                {
                    //Console.WriteLine("Alternative SVBL " + SVBLAddress.ToString("X8"));
                    SVBLFound = true;
                    SymbolHelper shsvbl = new SymbolHelper();
                    shsvbl.Category = "Detected maps";
                    shsvbl.Subcategory = "Limiters";
                    //shsvbl.StartAddress = SVBLAddress - 2;
                    shsvbl.StartAddress = SVBLAddress + 16;

                    // if value = 0xC3 0x00 -> two more back
                    int[] testValue = tools.readdatafromfileasint(@"D:\X_TUNING\DAMOS\Damos Pack for Winols 10GB\damos pack 1\DAMOS 1\DAMOS 1\VAG EDC15P 1.9TDI\0281010744.ORI", (int)shsvbl.StartAddress, shsvbl.Length);
                    int[] testValue1 = tools.convertBytesToInts(allBytes, (int)shsvbl.StartAddress, shsvbl.Length);

                    if (testValue[0] == 0xC300) shsvbl.StartAddress -= 2;

                    shsvbl.Varname = "SVBL Boost limiter [" + DetermineNumberByFlashBank(shsvbl.StartAddress, newCodeBlocks) + "]";
                    shsvbl.Length = 2;
                    shsvbl.CodeBlock = DetermineCodeBlockByByAddress(shsvbl.StartAddress, newCodeBlocks);
                    newSymbols.Add(shsvbl);

                    int MAPMAFSwitch = tools.findSequence(allBytes, SVBLAddress - 0x100, new byte[8] { 0x41, 0x02, 0xFF, 0xFF, 0x00, 0x01, 0x01, 0x00 }, new byte[8] { 1, 1, 0, 0, 1, 1, 1, 1 });
                    if (MAPMAFSwitch > 0)
                    {
                        MAPMAFSwitch += 2;
                        SymbolHelper mapmafsh = new SymbolHelper();
                        //mapmafsh.BitMask = 0x0101;
                        mapmafsh.Category = "Detected maps";
                        mapmafsh.Subcategory = "Switches";
                        mapmafsh.StartAddress = MAPMAFSwitch;
                        mapmafsh.Varname = "MAP/MAF switch (0 = MAF, 257/0x101 = MAP)" + DetermineNumberByFlashBank(shsvbl.StartAddress, newCodeBlocks);
                        mapmafsh.Length = 2;
                        mapmafsh.CodeBlock = DetermineCodeBlockByByAddress(mapmafsh.StartAddress, newCodeBlocks);
                        newSymbols.Add(mapmafsh);
                        //Console.WriteLine("Found MAP MAF switch @ " + MAPMAFSwitch.ToString("X8"));
                    }
                    offset = SVBLAddress + 1;
                }

                else found = false;
            }
            return SVBLFound;
        }

        private bool FindSVBLSequenceTwo(byte[] allBytes, SymbolCollection newSymbols, List<CodeBlock> newCodeBlocks)
        {
            bool found = true;
            bool SVBLFound = false;
            int offset = 0;
            while (found)
            {

                int SVBLAddress = tools.findSequence(allBytes, offset, new byte[10] { 0xDF, 0x7A, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0xDF, 0x7A }, new byte[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
                if (SVBLAddress > 0)
                {
                    SVBLFound = true;
                    SymbolHelper shsvbl = new SymbolHelper();
                    shsvbl.Category = "Detected maps";
                    shsvbl.Subcategory = "Limiters";
                    shsvbl.StartAddress = SVBLAddress - 2;
                    //shsvbl.StartAddress = SVBLAddress + 16;

                    // if value = 0xC3 0x00 -> two more back
                    //int[] testValue = tools.readdatafromfileasint(filename, (int)shsvbl.StartAddress, shsvbl.Length);
                    int[] testValue = tools.convertBytesToInts(allBytes, (int)shsvbl.StartAddress, shsvbl.Length);
                    if (testValue[0] == 0xC300) shsvbl.StartAddress -= 2;

                    shsvbl.Varname = "SVBL Boost limiter [" + DetermineNumberByFlashBank(shsvbl.StartAddress, newCodeBlocks) + "]";
                    shsvbl.Length = 2;
                    shsvbl.CodeBlock = DetermineCodeBlockByByAddress(shsvbl.StartAddress, newCodeBlocks);
                    newSymbols.Add(shsvbl);

                    int MAPMAFSwitch = tools.findSequence(allBytes, SVBLAddress - 0x100, new byte[8] { 0x41, 0x02, 0xFF, 0xFF, 0x00, 0x01, 0x01, 0x00 }, new byte[8] { 1, 1, 0, 0, 1, 1, 1, 1 });
                    if (MAPMAFSwitch > 0)
                    {
                        MAPMAFSwitch += 2;
                        SymbolHelper mapmafsh = new SymbolHelper();
                        //mapmafsh.BitMask = 0x0101;
                        mapmafsh.Category = "Detected maps";
                        mapmafsh.Subcategory = "Switches";
                        mapmafsh.StartAddress = MAPMAFSwitch;
                        mapmafsh.Varname = "MAP/MAF switch (0 = MAF, 257/0x101 = MAP)" + DetermineNumberByFlashBank(shsvbl.StartAddress, newCodeBlocks);
                        mapmafsh.Length = 2;
                        mapmafsh.CodeBlock = DetermineCodeBlockByByAddress(mapmafsh.StartAddress, newCodeBlocks);
                        newSymbols.Add(mapmafsh);
                        //Console.WriteLine("Found MAP MAF switch @ " + MAPMAFSwitch.ToString("X8"));
                    }


                    offset = SVBLAddress + 1;
                }
                else found = false;
            }
            return SVBLFound;
        }
        #endregion


        #region CodeBlocks

        /// <summary>
        /// serach for code blocks
        /// </summary>
        /// <param name="allBytes"></param>
        /// <param name="newCodeBlocks"></param>
        private void serachCodeBlocks(byte[] allBytes, List<CodeBlock> newCodeBlocks)
        {
            bool found = true;
            int offset = 0;
            int defaultCodeBlockLength = 0x10000;
            int currentCodeBlockLength = 0;
            int prevCodeBlockStart = 0;

            //serach for all codeblock patterns
            while (found)
            {
                int CodeBlockAddress = tools.findSequence(allBytes, offset, new byte[11] { 0xC1, 0x02, 0x00, 0x68, 0x00, 0x25, 0x03, 0x00, 0x00, 0x10, 0x27 }, new byte[11] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
                if (CodeBlockAddress > 0)
                {
                    CodeBlock newcodeblock = new CodeBlock();
                    newcodeblock.StartAddress = CodeBlockAddress - 1;
                    if (prevCodeBlockStart == 0) prevCodeBlockStart = newcodeblock.StartAddress;
                    else if (currentCodeBlockLength == 0)
                    {
                        currentCodeBlockLength = newcodeblock.StartAddress - prevCodeBlockStart;
                        if (currentCodeBlockLength > 0x10000) currentCodeBlockLength = 0x10000;
                    }
                    // find the next occurence of the checksum
                    newCodeBlocks.Add(newcodeblock);
                    offset = CodeBlockAddress + 1;
                }
                else found = false;
            }

            //determin start and end address of codeblocks
            foreach (CodeBlock cb in newCodeBlocks)
            {
                if (currentCodeBlockLength != 0) cb.EndAddress = cb.StartAddress + currentCodeBlockLength - 1;
                else cb.EndAddress = cb.StartAddress + defaultCodeBlockLength - 1;
            }


            foreach (CodeBlock cb in newCodeBlocks)
            {
                int autoSequenceIndex   = tools.findSequence(allBytes, cb.StartAddress, new byte[7] { 0x45, 0x44, 0x43, 0x20, 0x20, 0x41, 0x47 }, new byte[7] { 1, 1, 1, 1, 1, 1, 1 });
                int manualSequenceIndex = tools.findSequence(allBytes, cb.StartAddress, new byte[7] { 0x45, 0x44, 0x43, 0x20, 0x20, 0x53, 0x47 }, new byte[7] { 1, 1, 1, 1, 1, 1, 1 });

                if (autoSequenceIndex < cb.EndAddress && autoSequenceIndex >= cb.StartAddress)      cb.gearboxType = GearboxType.Automatic;
                if (manualSequenceIndex < cb.EndAddress && manualSequenceIndex >= cb.StartAddress)  cb.gearboxType = GearboxType.Manual;
            }


            if (tools.m_currentfilelength >= 0x80000)
            {
                CheckCodeBlock(0x50000, allBytes, newCodeBlocks); //manual specific
                CheckCodeBlock(0x60000, allBytes, newCodeBlocks); //automatic specific
                CheckCodeBlock(0x70000, allBytes, newCodeBlocks); //quattro specific
            }
        }

        private int CheckCodeBlock(int offset, byte[] allBytes, List<CodeBlock> newCodeBlocks)
        {
            int codeBlockID = 0;
            try
            {
                int endOfTable = Convert.ToInt32(allBytes[offset + 0x01000]) + Convert.ToInt32(allBytes[offset + 0x01001]) * 256 + offset;
                //sth wrong here with File 019AQ (ARL)
                int codeBlockAddress = Convert.ToInt32(allBytes[offset + 0x01002]) + Convert.ToInt32(allBytes[offset + 0x01003]) * 256 + offset;
                if (endOfTable == offset + 0xC3C3) return 0;
                codeBlockID = Convert.ToInt32(allBytes[codeBlockAddress]) + Convert.ToInt32(allBytes[codeBlockAddress + 1]) * 256;
                //Why do we need line obove?
                //codeBlockID = Convert.ToInt32(allBytes[codeBlockAddress]);

                foreach (CodeBlock cb in newCodeBlocks)
                {
                    if (cb.StartAddress <= codeBlockAddress && cb.EndAddress >= codeBlockAddress)
                    {
                        cb.CodeID = codeBlockID;
                        cb.AddressID = codeBlockAddress;
                    }
                }
            }
            catch (Exception)
            {
            }
            return codeBlockID;
        }

        #endregion
    }
}
