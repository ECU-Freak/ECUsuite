using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Collections;


namespace ECUsuite.Toolbox
{

    public enum XDFCategories : int
    {
        Undocumented = 0,
        Fuel,
        Ignition,
        Boost_control,
        Idle,
        Correction,
        Misc,
        Sensor,
        Runtime,
        Diagnostics
    }

    public enum CarMakes : int
    {
        Audi,
        BMW,
        Volkswagen,
        Unknown
    }

    public enum EngineType : int
    {
        cc1200,
        cc1400,
        cc1600,
        cc1900,
        cc2500
    }

    public enum ImportFileType : int
    {
        XML,
        A2L,
        CSV,
        AS2,
        Damos
    }

    public class Tools
    {
        private object syncRoot = new Object();

        //public EDCFileType m_currentFileType = EDCFileType.EDC15P; // default
        //public TransactionLog m_ProjectTransactionLog;
        public string m_CurrentWorkingProject = string.Empty;
        public ProjectLog m_ProjectLog = new ProjectLog();

        public string m_currentfile = string.Empty;
        public int m_currentfilelength = 0x80000;

        public string FindAscii(byte[] allBytes, int start, int end, int length)
        {
            for (int i = start; i < end; i++)
            {
                string testStr = System.Text.ASCIIEncoding.ASCII.GetString(allBytes, i, length);
                testStr = StripNonAsciiCapital(testStr);
                if (testStr.Length == length) return testStr;
            }
            return "";
        }

        public string FindDigits(byte[] allBytes, int start, int end, int length)
        {
            for (int i = start; i < end; i++)
            {
                string testStr = System.Text.ASCIIEncoding.ASCII.GetString(allBytes, i, length);
                testStr = StripNonDigit(testStr);
                if (testStr.Length == length) return testStr;
            }
            return "";
        }

        public string StripNonAsciiCapital(string input)
        {
            string retval = string.Empty;
            foreach (char c in input)
            {
                if (c >= 0x30 && c <= 0x39) retval += c;
                if (c >= 0x41 && c <= 0x5A) retval += c;
            }
            return retval;
        }

        public string StripNonDigit(string input)
        {
            string retval = string.Empty;
            foreach (char c in input)
            {
                if (c >= 0x30 && c <= 0x39) retval += c;
            }
            return retval;
        }

        public string StripNonAscii(string input)
        {
            string retval = string.Empty;
            foreach (char c in input)
            {
                if (c >= 0x30 && c <= 0x39) retval += c;
                else if (c >= 0x41 && c <= 0x5A) retval += c;
                else if (c >= 0x61 && c <= 0x7A) retval += c;
            }
            return retval;
        }

        public bool isLetter(char c)
        {
            if (c >= 0x41 && c <= 0x5A) return true;
            return false;
        }

        public bool isDigit(char c)
        {
            if (c >= 0x30 && c <= 0x39) return true;
            return false;
        }

        public string GetWorkingDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VAGEDCSuite");
        }

        /// <summary>
        /// compares a pattern to a specific position in a byte array
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="startIndex"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool CheckBytesAtPosition(byte[] byteArray, int startIndex, byte[] pattern)
        {
            // Safety check: Ensure the array and pattern are not null
            if (byteArray == null)
            {
                throw new ArgumentNullException(nameof(byteArray), "The byte array cannot be null.");
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern), "The pattern to check cannot be null.");
            }

            // Check if the startIndex and pattern length are within bounds of the byte array
            if (startIndex < 0 || startIndex + pattern.Length > byteArray.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index is out of bounds, or the pattern exceeds the array length.");
            }

            // Compare the bytes at the specified position
            for (int i = 0; i < pattern.Length; i++)
            {
                if (byteArray[startIndex + i] != pattern[i])
                {
                    return false; // Byte mismatch found, pattern does not match
                }
            }

            return true; // All bytes match the pattern
        }


        /// <summary>
        /// returns a string from a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public string extractString(byte[] data, int startIndex, int length)
        {
            //check if parameter match the bounds of the array
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Das Byte-Array darf nicht null sein.");
            }

            if (startIndex < 0 || startIndex >= data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Der Startindex liegt außerhalb des Byte-Arrays.");
            }

            if (length < 0 || (startIndex + length) > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Die Länge liegt außerhalb des Byte-Arrays.");
            }
            //convert to string
            return Encoding.UTF8.GetString(data, startIndex, length);
        }


        public int findSequence(byte[] fileData, int offset, byte[] sequence, byte[] mask)
        {

            byte data;
            int i, max;
            i = 0;
            max = 0;
            int position = offset;

            while (position < fileData.Length)
            {
                data = (byte)fileData[position++];
                if (data == sequence[i] || mask[i] == 0)
                {
                    i++;
                }
                else
                {
                    if (i > max) max = i;
                    position -= i;
                    i = 0;
                }
                if (i == sequence.Length) break;
            }
            if (i == sequence.Length)
            {
                return ((int)position - sequence.Length);
            }
            else
            {
                return -1;
            }
        }

        public int findSequence(byte[] fileData, int offset, byte[] sequence)
        {

            byte data;
            int i, max;
            i = 0;
            max = 0;
            int position = offset;

            while (position < fileData.Length)
            {
                data = (byte)fileData[position++];
                if (data == sequence[i])
                {
                    i++;
                }
                else
                {
                    if (i > max) max = i;
                    position -= i;
                    i = 0;
                }
                if (i == sequence.Length) break;
            }

            if (i == sequence.Length)
            {
                return ((int)position - sequence.Length);
            }
            else
            {
                return -1;
            }
        }




        /// <summary>
        /// Function to find the pattern in byte array
        /// LE -> Less or equal
        /// GE -> Greater or equal
        /// example1: 0x01 0x02 GE03 -> third byte can be 0x03 or greater
        /// example2: 0x01 0x02 LE03 -> third byte can be 0x03 or less
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public int findPattern(byte[] data, int offset, string hexPattern, bool endAddr = false)
        {
            // Konvertiere den Hex-String in ein PatternRule[]-Array
            PatternRule[] pattern = ConvertHexPatternToRules(hexPattern);

            // Stelle sicher, dass der Offset im gültigen Bereich liegt
            if (offset < 0 || offset > data.Length - pattern.Length)
            {
                return -1; // Ungültiger Offset, Rückgabe -1
            }

            // Iteriere über das Byte-Array ab dem Offset
            for (int i = offset; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;

                // Prüfe, ob das Muster an der aktuellen Position passt
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (!pattern[j].Matches(data[i + j]))
                    {
                        match = false;
                        break;
                    }
                }

                // Wenn das Muster passt, gib den Startindex zurück
                if (match)
                {
                    //if we want the end address of the pattern, add this to the index
                    if(endAddr)
                    {
                        i += pattern.Length;
                    }

                    //return address
                    return i;
                }
            }

            // Wenn kein Muster gefunden wird, gib -1 zurück
            return -1;
        }


        // Funktion zur Konvertierung eines Hex-String-Musters in ein PatternRule[]-Array
        public static PatternRule[] ConvertHexPatternToRules(string hexPattern)
        {
            // Teile den Hex-String an den Leerzeichen
            string[] hexBytes = hexPattern.Split(' ');

            // Erstelle ein PatternRule[]-Array für das Muster
            PatternRule[] pattern = new PatternRule[hexBytes.Length];

            for (int i = 0; i < hexBytes.Length; i++)
            {
                if (hexBytes[i].StartsWith("GE")) // Greater or equal rule (e.g., GE05)
                {
                    // Extrahiere die Zahl und erstelle eine GreaterOrEqualRule
                    int value = Convert.ToInt32(hexBytes[i].Substring(2), 16);
                    pattern[i] = new GreaterOrEqualRule(value);
                }
                else if (hexBytes[i].StartsWith("LE")) // Greater or equal rule (e.g., GE05)
                {
                    // Extrahiere die Zahl und erstelle eine GreaterOrEqualRule
                    int value = Convert.ToInt32(hexBytes[i].Substring(2), 16);
                    pattern[i] = new LessOrEqualRule(value);
                }

                else if (hexBytes[i].StartsWith("0x")) // Exact byte rule (e.g., 0x00)
                {
                    try
                    {
                        // Konvertiere den Hex-String in ein Byte und erstelle eine ExactByteRule
                        byte value = Convert.ToByte(hexBytes[i].Replace("0x", ""), 16);
                        pattern[i] = new ExactByteRule(value);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    throw new ArgumentException("Invalid pattern format: " + hexBytes[i]);
                }
            }

            return pattern;
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="filename"></param>
        /// <param name="DoTransActionEntry"></param>
        /// <param name="reverseEdian"></param>
        public void savedatatobinary(int address, int length, byte[] data, string filename, bool DoTransActionEntry, bool reverseEdian = true)
        {
            // depends on filetype (EDC16 is not reversed)
            if (reverseEdian)
            {
                data = reverseEndian(data);
            }
            if (address > 0 && address < m_currentfilelength)
            {
                try
                {
                    byte[] beforedata = readdatafromfile(filename, address, length, reverseEdian);
                    FileStream fsi1 = File.OpenWrite(filename);
                    BinaryWriter bw1 = new BinaryWriter(fsi1);
                    fsi1.Position = address;
                    for (int i = 0; i < length; i++)
                    {
                        bw1.Write((byte)data.GetValue(i));
                    }
                    fsi1.Flush();
                    bw1.Close();
                    fsi1.Close();
                    fsi1.Dispose();
                }
                catch (Exception E)
                {
                   // MessageBox.Show("Failed to write to binary. Is it read-only? Details: " + E.Message);
                }
            }
        }

        /// <summary>
        /// on edc 16 reverseEdian has to be false
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="reverseEdian"></param>
        /// <returns></returns>
        public byte[] readdatafromfile(string filename, int address, int length, bool reverseEdian = true)
        {
            byte[] retval = new byte[length];
            try
            {
                FileStream fsi1 = File.OpenRead(filename);
                while (address > fsi1.Length) address -= (int)fsi1.Length;
                BinaryReader br1 = new BinaryReader(fsi1);
                fsi1.Position = address;
                string temp = string.Empty;
                for (int i = 0; i < length; i++)
                {
                    retval.SetValue(br1.ReadByte(), i);
                }
                // depends on filetype (EDC16 is not reversed)
                if (reverseEdian)
                {
                    retval = reverseEndian(retval);
                }
                fsi1.Flush();
                br1.Close();
                fsi1.Close();
                fsi1.Dispose();
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
            }
            return retval;
        }


        /// <summary>
        /// on edc 16 reverseEdian has to be false
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="reverseEdian"></param>
        /// <returns></returns>
        public int[] readdatafromfileasint(string filename, int address, int length, bool reverseEdian = true)
        {
            int[] retval = new int[length];
            FileStream fsi1 = File.OpenRead(filename);
            while (address > fsi1.Length) address -= (int)fsi1.Length;
            BinaryReader br1 = new BinaryReader(fsi1);
            fsi1.Position = address;
            string temp = string.Empty;

            for (int i = 0; i < length; i++)
            {
                if (reverseEdian)
                {
                    int iVal = Convert.ToInt32(br1.ReadByte());
                    iVal += Convert.ToInt32(br1.ReadByte()) * 256;
                    retval.SetValue(iVal, i);
                }
                else
                {
                    int iVal = Convert.ToInt32(br1.ReadByte()) * 256;
                    iVal += Convert.ToInt32(br1.ReadByte());
                    retval.SetValue(iVal, i);
                }
            }
            // little endian, reverse bytes
            //retval = reverseEndian(retval);
            fsi1.Flush();
            br1.Close();
            fsi1.Close();
            fsi1.Dispose();
            return retval;
        }

        /// <summary>
        /// convert byte array to int array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="reverseEndian"></param>
        /// <returns></returns>
        public int[] convertBytesToInts(byte[] data, int address, int length, bool reverseEndian = true)
        {
            int[] retval = new int[length];

            // Ensure the address is within the bounds of the data array
            while (address > data.Length) address -= data.Length;

            for (int i = 0; i < length; i++)
            {
                if (reverseEndian)
                {
                    int iVal = Convert.ToInt32(data[address]);
                    iVal += Convert.ToInt32(data[address + 1]) * 256;
                    retval.SetValue(iVal, i);
                }
                else
                {
                    int iVal = Convert.ToInt32(data[address]) * 256;
                    iVal += Convert.ToInt32(data[address + 1]);
                    retval.SetValue(iVal, i);
                }

                // Move the address forward by 2 bytes (since we are reading two bytes per iteration)
                address += 2;

                // Ensure we do not go out of bounds of the data array
                if (address >= data.Length)
                {
                    break;
                }
            }

            return retval;
        }

        public byte[] reverseEndian(byte[] retval)
        {
            byte[] ret = new byte[retval.Length];

            try
            {
                if (retval.Length > 0 && retval.Length % 2 == 0)
                {
                    for (int i = 0; i < retval.Length; i += 2)
                    {
                        byte b1 = retval[i];
                        byte b2 = retval[i + 1];
                        ret[i] = b2;
                        ret[i + 1] = b1;
                    }
                }
            }
            catch (Exception E)
            {

            }
            return ret;
        }

        public int[] reverseEndian(int[] retval)
        {
            int[] ret = new int[retval.Length];

            try
            {
                if (retval.Length > 0 && retval.Length % 2 == 0)
                {
                    for (int i = 0; i < retval.Length; i += 2)
                    {
                        int b1 = retval[i];
                        int b2 = retval[i + 1];
                        ret[i] = b2;
                        ret[i + 1] = b1;
                    }
                }
            }
            catch (Exception E)
            {

            }
            return ret;
        }


    }

    // Basis-Klasse für Regel
    public abstract class PatternRule
    {
        public abstract bool Matches(byte b);
    }

    // Regel für exakte Übereinstimmung
    public class ExactByteRule : PatternRule
    {
        private byte _value;

        public ExactByteRule(byte value)
        {
            _value = value;
        }

        public override bool Matches(byte b)
        {
            return b == _value;
        }
    }

    // Regel für "Greater or Equal" (größer oder gleich)
    public class GreaterOrEqualRule : PatternRule
    {
        private int _value;

        public GreaterOrEqualRule(int value)
        {
            _value = value;
        }

        public override bool Matches(byte b)
        {
            return b >= _value;
        }
    }

    public class LessOrEqualRule : PatternRule
    {
        private int _value;

        public LessOrEqualRule(int value)
        {
            _value = value;
        }

        public override bool Matches(byte b)
        {
            return b <= _value;
        }
    }

}
