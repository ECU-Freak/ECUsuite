using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace OlsParser
{
    public class WinOlsParser
    {
        private Tools tools = new Tools();
        private string filePath { get; set; } = string.Empty;
        private byte[] fileData { get; set; } = new byte[0];

        /// <summary>
        /// read .ols file and save to byte array
        /// </summary>
        /// <param name="filePath"></param>
        public void readFile(string filePath)
        {
            if (filePath == null) filePath = this.filePath;

            this.fileData = System.IO.File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// extract symbols from ols file
        /// </summary>
        /// <returns></returns>
        public SymbolCollection extractSymbols()
        {
            SymbolCollection symbols = new SymbolCollection();

            //get parser for right file version
            IOlsParser olsParser = getOlsParser(fileData);
            
            //first find ols symbols
            List<OlsSymbol> olsSymbols = olsParser.findOlsSymbols(fileData);

            //iterate through the ols symbols and convert to ecuSuite symbols
            foreach (OlsSymbol olsSymbol in olsSymbols)
            {
                SymbolHelper symbol = olsParser.convertOlsSymbol(olsSymbol);
                symbols.Add(symbol);
            }

            return symbols;
        }


        /// <summary>
        /// export to csv
        /// </summary>
        /// <param name="symbol"></param>
        public void saveToCsv(SymbolCollection symbols)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "export.csv";


            if(sfd.ShowDialog() == true)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(symbols.ToList()[0].getCsvHeader());

                foreach (SymbolHelper sym in symbols)
                {
                    sb.AppendLine(sym.ToCsv());
                }
                
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString()); 
            }
        }

        /// <summary>
        /// export to .cs, may unusable if multiple thousands of symbols -> use xml
        /// </summary>
        /// <param name="symbol"></param>
        public void saveToDotCs(SymbolCollection symbols)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "symbols.cs";


            if (sfd.ShowDialog() == true)
            {
                StringBuilder sb = new StringBuilder();

                // Header für die generierte Datei
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine();
                sb.AppendLine("public static class SymbolHelperData");
                sb.AppendLine("{");
                sb.AppendLine("    public static List<SymbolHelper> GetSymbols()");
                sb.AppendLine("    {");
                sb.AppendLine("        return new List<SymbolHelper>");
                sb.AppendLine("        {");

                foreach (SymbolHelper symbol in symbols)
                {
                    sb.AppendLine("            new SymbolHelper");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                Varname = \"{symbol.Varname}\",");
                    sb.AppendLine($"                NickName = \"{symbol.NickName}\",");
                    sb.AppendLine($"                Description = \"{symbol.Description}\",");
                    sb.AppendLine($"                Unit = \"{symbol.Unit}\",");
                    sb.AppendLine($"                Id = \"{symbol.Id}\",");
                    sb.AppendLine($"                StartAddress = {symbol.StartAddress},");
                    sb.AppendLine($"                Type = {symbol.Type},");
                    sb.AppendLine($"                Length = {symbol.Length},");

                    // factor
                    sb.AppendLine("                Factor = new SymbolHelper.Ffactor");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    Signed = {symbol.factor.signed.ToString().ToLower()},");
                    sb.AppendLine($"                    Bitness = SymbolHelper.Bitness.{symbol.factor.bitness},");
                    sb.AppendLine($"                    FactorValue = {symbol.factor.factor},");
                    sb.AppendLine($"                    Offset = {symbol.factor.offset},");
                    sb.AppendLine($"                    ReciprocalFactor = {symbol.factor.reciprocalFactor},");
                    sb.AppendLine($"                    Reciprocal = {symbol.factor.reciprocal.ToString().ToLower()},");
                    sb.AppendLine($"                    PrePrecision = {symbol.factor.prePrecision},");
                    sb.AppendLine($"                    PostPrecision = {symbol.factor.postPrecision}");
                    sb.AppendLine("                },");

                    // X-Axis
                    sb.AppendLine("                Xaxis = new thisHelper.thisAxis");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    Description = \"{symbol.Xaxis.Description}\",");
                    sb.AppendLine($"                    Unit = \"{symbol.Xaxis.Unit}\",");
                    sb.AppendLine($"                    Id = \"{symbol.Xaxis.Id}\",");
                    sb.AppendLine($"                    StartAddress = {symbol.Xaxis.StartAddress},");
                    sb.AppendLine($"                    Length = {symbol.Xaxis.Length},");
                    sb.AppendLine("                    Factor = new SymbolHelper.Ffactor");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        Signed = {symbol.Xaxis.factor.signed.ToString().ToLower()},");
                    sb.AppendLine($"                        Bitness = SymbolHelper.Bitness.{symbol.Xaxis.factor.bitness},");
                    sb.AppendLine($"                        FactorValue = {symbol.Xaxis.factor.factor},");
                    sb.AppendLine($"                        Offset = {symbol.Xaxis.factor.offset},");
                    sb.AppendLine($"                        ReciprocalFactor = {symbol.Xaxis.factor.reciprocalFactor},");
                    sb.AppendLine($"                        Reciprocal = {symbol.Xaxis.factor.reciprocal.ToString().ToLower()},");
                    sb.AppendLine($"                        PrePrecision = {symbol.Xaxis.factor.prePrecision},");
                    sb.AppendLine($"                        PostPrecision = {symbol.Xaxis.factor.postPrecision}");
                    sb.AppendLine("                    }");
                    sb.AppendLine("                },");

                    // Y-Axis
                    sb.AppendLine("                Yaxis = new SymbolHelper.SymbolAxis");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    Description = \"{symbol.Yaxis.Description}\",");
                    sb.AppendLine($"                    Unit = \"{symbol.Yaxis.Unit}\",");
                    sb.AppendLine($"                    Id = \"{symbol.Yaxis.Id}\",");
                    sb.AppendLine($"                    StartAddress = {symbol.Yaxis.StartAddress},");
                    sb.AppendLine($"                    Length = {symbol.Yaxis.Length},");
                    sb.AppendLine("                    Factor = new SymbolHelper.Ffactor");
                    sb.AppendLine("                    {");
                    sb.AppendLine($"                        Signed = {symbol.Yaxis.factor.signed.ToString().ToLower()},");
                    sb.AppendLine($"                        Bitness = SymbolHelper.Bitness.{symbol.Yaxis.factor.bitness},");
                    sb.AppendLine($"                        FactorValue = {symbol.Yaxis.factor.factor},");
                    sb.AppendLine($"                        Offset = {symbol.Yaxis.factor.offset},");
                    sb.AppendLine($"                        ReciprocalFactor = {symbol.Yaxis.factor.reciprocalFactor},");
                    sb.AppendLine($"                        Reciprocal = {symbol.Yaxis.factor.reciprocal.ToString().ToLower()},");
                    sb.AppendLine($"                        PrePrecision = {symbol.Yaxis.factor.prePrecision},");
                    sb.AppendLine($"                        PostPrecision = {symbol.Yaxis.factor.postPrecision}");
                    sb.AppendLine("                    }");
                    sb.AppendLine("                },");
                }


                // Schließe die Liste und Funktion ab
                sb.AppendLine("        };");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
            }
        }

        /// <summary>
        /// export symbols to xml
        /// </summary>
        /// <param name="symbols"></param>
        public void saveToXml(SymbolCollection symbols)
        {
            List<SymbolHelper> symbolList = symbols.ToList();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "export.xml";

            symbolList.RemoveAt(0);

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<SymbolHelper>));

                    using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
                    {
                        serializer.Serialize(stream, symbolList);
                    }


                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {

                    }

                    Console.WriteLine("Die Liste wurde erfolgreich als XML serialisiert: " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Serialisieren: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// save to binary
        /// </summary>
        /// <param name="symbols"></param>
        public void saveToBin(SymbolCollection symbols)
        {
            List<SymbolHelper> symbolList = symbols.ToList();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "export.ecuprj";

            symbolList.RemoveAt(0);

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    Symbols syms = new Symbols();
                    SymbolFileHandler sfh = new SymbolFileHandler();

                    syms.symbolList = symbols.ToList();

                    sfh.save(sfd.FileName, syms);

                    Console.WriteLine("Die Liste wurde erfolgreich als XML serialisiert: " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Serialisieren: " + ex.Message);
                }
            }
        }


        /// <summary>
        /// returns the correct ols parser
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IOlsParser getOlsParser(byte[] fileData)
        {
            string olsVersion = getOlsVersion(fileData);

            IOlsParser parser = new OLS_5502();

            switch (olsVersion)
            {
                case "5502":
                    parser = new OLS_5502();
                    break;
                case "FA00":
                    throw new Exception("FA00 is not supported yet");
                    break;
                case "6A00":
                    throw new Exception("6A00 is not supported yet");
                    break;
                case "6400":
                    throw new Exception("6400 is not supported yet");
                    break;
                case "7100":
                    throw new Exception("7100 is not supported yet");
                    break;
                case "2401":
                    throw new Exception("2401 is not supported yet");
                    break;
                default:
                    throw new Exception("Unknown OLS version: " + olsVersion);
                    break;
            }

            return parser;
        }

        /// <summary>
        /// get ols version from file as string
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns></returns>
        private string getOlsVersion(byte[] fileData)
        {
            return fileData[16].ToString("X2") + fileData[17].ToString("X2");
        }
    }
}
