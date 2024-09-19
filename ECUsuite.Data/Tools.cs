using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;


namespace ECUsuite.Tools
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
        public int m_codeBlock5ID = 0;
        public int m_codeBlock6ID = 0;
        public int m_codeBlock7ID = 0;
        public CarMakes m_carMake = CarMakes.Unknown; // used for EDC17
        //public List<CodeBlock> codeBlockList = new List<CodeBlock>();
        //public SymbolCollection m_symbols = new SymbolCollection();
        //public List<AxisHelper> AxisList = new List<AxisHelper>();

        public int TorqueToPowerkW(int torque, int rpm)
        {
            double power = (torque * rpm) / 7121;
            // convert to kW in stead of horsepower
            power *= 0.73549875;
            return Convert.ToInt32(power);
        }

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

        public int PowerToTorque(int power, int rpm)
        {
            double torque = (power * 7121) / rpm;
            return Convert.ToInt32(torque);
        }

        public int TorqueToPower(int torque, int rpm)
        {
            double power = (torque * rpm) / 7121;
            return Convert.ToInt32(power);
        }

        public double GetCorrectionFactorForRpm(int rpm, int numberCylinders)
        {
            double correction = 1;
            if (numberCylinders == 6)
            {
                if (rpm >= 4000) correction = 0.80;
                else if (rpm >= 3500) correction = 0.90;
                else if (rpm >= 3250) correction = 0.90;
                else if (rpm >= 3000) correction = 0.93;
                else if (rpm >= 2500) correction = 0.90;
                else if (rpm >= 2250) correction = 0.90;
                else if (rpm >= 1700) correction = 0.90;
                else correction = 0.9;
            }
            else
            {
                if (rpm >= 4000) correction = 0.75;
                else if (rpm >= 3500) correction = 0.83;
                else if (rpm >= 3250) correction = 0.89;
                else if (rpm >= 3000) correction = 0.96;
                else if (rpm >= 2500) correction = 0.98;
                else if (rpm >= 2250) correction = 0.99;
                else correction = 1.00;
            }
            return correction;

        }

        public int IQToTorque(int IQ, int rpm, int numberCylinders)
        {
            double tq = Convert.ToDouble(IQ) * 6;

            // correct for number of cylinders
            tq *= numberCylinders;
            tq /= 4;

            double correction = GetCorrectionFactorForRpm(rpm, numberCylinders);
            tq *= correction;
            return Convert.ToInt32(tq);
        }

        public int TorqueToIQ(int torque, int rpm, int numberCylinders)
        {
            double iq = Convert.ToDouble(torque) / 6;

            // correct for number of cylinders
            iq *= 4;
            iq /= numberCylinders;
            

            double correction = GetCorrectionFactorForRpm(rpm, numberCylinders);
            iq /= correction;
            return Convert.ToInt32(iq);
        }

        public string GetWorkingDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VAGEDCSuite");
        }


        public void savedatatobinary(int address, int length, byte[] data, string filename, bool DoTransActionEntry, string note, bool reverseEdian = true)
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
                    /*

                    if (m_ProjectTransactionLog != null && DoTransActionEntry)
                    {
                        // depends on filetype (EDC16 is not reversed)
                        if (reverseEdian)
                        {
                            data = reverseEndian(data);
                        }
                        TransactionEntry tentry = new TransactionEntry(DateTime.Now, address, length, beforedata, data, 0, 0, note);
                        m_ProjectTransactionLog.AddToTransactionLog(tentry);
                        if (m_CurrentWorkingProject != string.Empty)
                        {
                            //m_ProjectLog.WriteLogbookEntry(LogbookEntryType.TransactionExecuted, GetSymbolNameByAddress(address) + " " + note);
                        }
                    }
                    */
                }
                catch (Exception E)
                {
                    //WinInfoBox info = new WinInfoBox("Failed to write to binary. Is it read-only? Details: " + E.Message);
                    MessageBox.Show("Failed to write to binary. Is it read-only? Details: " + E.Message);
                }
            }
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
    }
}
