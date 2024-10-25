using ECUsuite.ECU.Base;
using ECUsuite.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECUsuite.Toolbox;
using System.Diagnostics;


namespace OlsParser
{
    public  class OLS_5502 : IOlsParser
    {
        private Tools tools = new Tools();

        public List<OlsSymbol> findOlsSymbols(byte[] fileData)
        {
            List<OlsSymbol> olsSymbols = new List<OlsSymbol>();
            int offset = 0;

            string olsSignature = "0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00" +
                                    "0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00" +
                                    "0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00" +
                                    "0x00 0x00 0x00 0x00 0x00 0xFF 0xFF 0xFF 0xFF";

            //find first start pattern
            offset = tools.findPattern(fileData, offset, olsSignature, true) - 4;


            while (offset < fileData.Length)
            {
                OlsSymbol olsSymbol = new OlsSymbol();

                //find start pattern

                olsSymbol.startAddress = offset;
                olsSymbol.endAddress = tools.findPattern(fileData, offset, olsSignature);

                olsSymbol.length = olsSymbol.endAddress - olsSymbol.startAddress;

                offset += olsSymbol.length + 62;


                //check if we reached end or continue
                if (olsSymbol.startAddress == -1 || olsSymbol.endAddress == -1)
                {
                    offset = fileData.Length + 1;
                }
                else
                {
                    //copy data
                    olsSymbol.data = new byte[olsSymbol.length];
                    Array.Copy(fileData, olsSymbol.startAddress, olsSymbol.data, 0, olsSymbol.length);

                    olsSymbols.Add(olsSymbol);
                }
            }
            return olsSymbols;
        }

        public SymbolHelper convertOlsSymbol(OlsSymbol olsSymbol)
        {
            //offsets are connected to each other, be carefull if you add additional values

            SymbolHelper symbol = new SymbolHelper();

            int startOffset = 0;
            int lengthOffset = 0;
            int endOffset = 0;

            ///###################################################################
            ///parse SYMBOL propertie
            ///###################################################################
            //Name, begins at offset 33, with length on offset 29
            startOffset = 33;
            lengthOffset = 29;
            symbol.Varname = extractString(olsSymbol.data, startOffset, lengthOffset, out endOffset);
            olsSymbol.addr_NameEnd = startOffset + olsSymbol.data[lengthOffset];

            //Name, seconde language
            //sperated by "02 00 00 00 02 00 00 00 01 00 00 00"
            startOffset = olsSymbol.addr_NameEnd + 16;
            lengthOffset = olsSymbol.addr_NameEnd + 12;
            olsSymbol.addr_NameEnd = startOffset + olsSymbol.data[lengthOffset];
            //Type
            startOffset = olsSymbol.addr_NameEnd + 4;
            symbol.Type = olsSymbol.data[startOffset];

            //Bitness
            startOffset = olsSymbol.addr_NameEnd + 12;
            symbol.factor.bitness = (Bitness)olsSymbol.data[startOffset];

            //Number Format
            startOffset = olsSymbol.addr_NameEnd + 20;
            int numberFormat = olsSymbol.data[startOffset];

            //ID, begins 132 bytes after Name, length 13 bytes after name
            startOffset = olsSymbol.addr_NameEnd + 32;
            lengthOffset = olsSymbol.addr_NameEnd + 28;
            symbol.Id = extractString(olsSymbol.data, startOffset, lengthOffset, out endOffset);
            olsSymbol.addr_IdEnd = startOffset + olsSymbol.data[lengthOffset];

            //number of rows
            startOffset = olsSymbol.addr_IdEnd + 121;
            symbol.Yaxis.Length = olsSymbol.data[startOffset];

            //number of columns
            startOffset = olsSymbol.addr_IdEnd + 117;
            symbol.Xaxis.Length = olsSymbol.data[startOffset];

            //postcomma
            startOffset = olsSymbol.addr_IdEnd + 133;
            symbol.factor.postPrecision = olsSymbol.data[startOffset];

            //Description
            startOffset = olsSymbol.addr_IdEnd + 141;
            lengthOffset = olsSymbol.addr_IdEnd + 137;
            symbol.Description = extractString(olsSymbol.data, startOffset, lengthOffset, out endOffset);
            olsSymbol.addr_DescrEnd = startOffset + olsSymbol.data[lengthOffset];

            //Unit
            startOffset = olsSymbol.addr_DescrEnd + 4;
            lengthOffset = olsSymbol.addr_DescrEnd;
            symbol.Unit = extractString(olsSymbol.data, startOffset, lengthOffset, out endOffset);
            olsSymbol.addr_UnitEnd = endOffset;

            //Factor
            startOffset = olsSymbol.addr_UnitEnd + 0;
            symbol.factor.factor = extractDouble(olsSymbol.data, startOffset, out endOffset);

            //Offset
            startOffset = olsSymbol.addr_UnitEnd + 8;
            symbol.factor.offset = extractDouble(olsSymbol.data, startOffset, out endOffset);

            //Address
            startOffset = olsSymbol.addr_UnitEnd + 16;
            symbol.StartAddress = extractAddress(olsSymbol.data, startOffset, out endOffset);


            ///###################################################################
            ///parse AXIS properties
            ///###################################################################
            if (symbol.Yaxis.Length > 1 || symbol.Xaxis.Length > 1)
            {
                //set inital offset for 
                int axisOffset = olsSymbol.addr_UnitEnd + 60;
                SymbolAxis axis;


                for (int i = 0; i < 2; i++)
                {
                    if (i == 0)
                    {
                        axis = symbol.Yaxis;
                    }
                    else
                    {
                        axis = symbol.Xaxis;
                    }

                    try
                    {

                        //axis name
                        axis.Description = extractString(olsSymbol.data, axisOffset, axisOffset - 4, out endOffset);
                        if (tools.CheckBytesAtPosition(olsSymbol.data, endOffset, new byte[] { 0x02, 0x00, 0x00, 0x00, 0x02 }))
                        {
                            startOffset = endOffset + 16;
                            string desc2 = extractString(olsSymbol.data, startOffset, startOffset - 4, out endOffset);
                        }

                        //unit
                        startOffset = endOffset + 8;
                        axis.Unit = extractString(olsSymbol.data, startOffset, startOffset - 4, out endOffset);

                        //factor
                        startOffset = endOffset;
                        axis.factor.factor = extractDouble(olsSymbol.data, startOffset, out endOffset);

                        //offset
                        startOffset = endOffset;
                        axis.factor.offset = extractDouble(olsSymbol.data, startOffset, out endOffset);

                        //address
                        startOffset = endOffset + 4;
                        axis.StartAddress = extractAddress(olsSymbol.data, startOffset, out endOffset);

                    }
                    catch
                    { }

                    //go to next axis
                    axisOffset = endOffset + 59;
                }
            }

            return symbol;
        }

        public byte[] getBinary()
        {
            byte[] retValue = new byte[0];


            return retValue;
        }

        private byte[] getData(byte[] data, string startPattern, string endPattern, ref int offset)
        {
            byte[] retValue = new byte[0];

            int start = offset;

            //if startpattern is set, serach for it, else, use offset as start to serach end pattern
            if (startPattern != "")
            {
                start = tools.findPattern(data, offset, startPattern, true);
                offset = start;
            }

            int stop = tools.findPattern(data, offset, endPattern);
            offset = stop;

            //check for valid results
            if (start >= 0 && stop >= 0 && stop > start)
            {
                int len = stop - start;
                retValue = new byte[len];

                Array.Copy(data, start, retValue, 0, len);
            }

            return retValue;
        }


        /// <summary>
        /// convert byte sequence to double (IEEE 754 64bit floating point)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        /// <returns></returns>
        private double extractDouble(byte[] data, int startOffset, out int endOffset)
        {
            endOffset = startOffset + 8;
            return BitConverter.ToDouble(data, startOffset);

        }

        /// <summary>
        /// extract int32 address
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startOffset"></param>
        /// <param name="endOffset"></param>
        /// <returns></returns>
        private int extractAddress(byte[] data, int startOffset, out int endOffset)
        {
            endOffset = startOffset + 4;
            return BitConverter.ToInt32(data, startOffset);
        }

        /// <summary>
        /// extract string from winols file format, need pos of first byte of string 
        /// and pos of the length byte, usally startpos - 4
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startOffset"></param>
        /// <param name="lengthOffset"></param>
        /// <param name="endOffset"></param>
        /// <returns></returns>
        private string extractString(byte[] data, int startOffset, int lengthOffset, out int endOffset)
        {
            string retValue;
            endOffset = lengthOffset + 4;

            if (tools.CheckBytesAtPosition(data, lengthOffset, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }))
            {
                retValue = "-";
            }
            else if (tools.CheckBytesAtPosition(data, lengthOffset, new byte[] { 0x00, 0x00, 0x00, 0x00 }))
            {
                retValue = "";
            }
            else if (tools.CheckBytesAtPosition(data, lengthOffset, new byte[] { 0xFD, 0xFF, 0xFF, 0xFF }))
            {
                retValue = "%";
            }
            else if (tools.CheckBytesAtPosition(data, lengthOffset, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF }))
            {
                retValue = "?";
            }
            else
            {
                int length = data[lengthOffset];

                retValue = tools.extractString(data, startOffset, length);
                endOffset = startOffset + length;
            }

            return retValue;
        }


    }
}
