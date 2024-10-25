using MemoryPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Navigation;
using MemoryPack;

namespace ECUsuite.ECU.Base
{

    public enum Bitness { Bit8 = 0x01, Bit16LoHi = 0x03, Bit16HiLo = 0x02, Bit32, Bit64 }
    public enum DataType {single, oneDim, twoDim, twoInv }

    [MemoryPackable]
    public partial class SymbolHelper
    {

        [Description("OEM Name of the variable")]
        public string Varname { get; set; } = string.Empty;

        [Description("General NickName of the variable like N75, Torque limiter")]
        public string NickName { get; set; } = string.Empty;

        [Description("Additional comments for symbol")]
        public string Description { get; set; } = string.Empty;

        [Description("Unit of the symbol values")]
        public string Unit { get; set; } = string.Empty;

        [Description("Id of the symbol, used for function description")]
        /// <summary>
        /// id from ecu description, like zmwMKOR_KF
        /// </summary>
        public string Id { get; set; } = string.Empty;
        [Description("Address where content start in flash, 1Byte base")]
        public int StartAddress { get; set; }

        [Description("Defines the type of the map, 1D, 2D, etc.")]
        public int Type { get; set; } = 0;

        [Description("Length of the symbol in bytes")]
        public int Length { get; set; }

        [Description("Factors to represent the data")]
        public Ffactor      factor { get; set; } = new Ffactor();

        [Description("Xaxis description of the symbol")]
        public SymbolAxis   Xaxis { get; set; } = new SymbolAxis();

        [Description("Yaxis description of the symbol")]
        public SymbolAxis   Yaxis { get; set; } = new SymbolAxis();

        public string Comment { get; set; } = string.Empty;

        #region MISC
        //Do not use, legacy
        public int CodeBlock { get; set; }
        //Do not use, legacy
        public MapSelector MapSelector { get; set; }
        #endregion

        #region additional functions
        [Description("Defines the path to the symbol in Project Explorer")]
        public string Path { get; set; } = string.Empty;

        private ObservableCollection<SymbolHelper> _children;

        [Description("Contains Children of this symbol")]
        public ObservableCollection<SymbolHelper> Children
        {
            get { return _children; }
            set { _children = value; }
        }

        [Description("returns the address of the symbol as hex string")]
        public string SymboAddressHex
        {
            get
            {
                if (StartAddress == 0) return string.Empty;
                return StartAddress.ToString("X");
            }
        }

        [Description("returns the size of the symbol as string X Y")]
        public string SymbolSizeXY
        {
            get
            {
                if (Xaxis.Length == 0 || Yaxis.Length == 0) return string.Empty;
                return Xaxis.Length.ToString() + "x" + Yaxis.Length.ToString();
            }
        }

        [Description("icon for solution explorer")]
        public string ICON { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Varname} | {SymboAddressHex} | {SymbolSizeXY} | {Description}";
        }

        public string Category { get; set; } = "Unknown maps";
        public string Subcategory { get; set; } = string.Empty;

        /// <summary>
        /// convert the data to csv format
        /// </summary>
        /// <returns></returns>
        public string ToCsv()
        {
            string csvRow = $"{this.Varname};" +
                            $"{this.NickName};" +
                            $"{this.Description};" +
                            $"{this.Unit};" +
                            $"{this.Id};" +
                            $"{this.StartAddress};" +
                            $"{this.Type};" +
                            $"{this.Length};" +
                            $"{this.factor.factor};" +            // Hauptfaktor
                            $"{this.factor.signed};" +            // Signiert
                            $"{this.factor.bitness};" +           // Bitness (z.B. 8-Bit; 16-Bit)
                            $"{this.factor.offset};" +            // Offset
                            $"{this.factor.reciprocal};" +        // Reziprok
                            $"{this.factor.prePrecision};" +      // Vor-Präzision
                            $"{this.factor.postPrecision};" +     // Nach-Präzision

                            // X-Achse
                            $"{this.Xaxis.Length};" +             // X-Achse Länge
                            $"{this.Xaxis.StartAddress};" +       // X-Achse Startadresse
                            $"{this.Xaxis.Unit};" +               // X-Achse Einheit
                            $"{this.Xaxis.Description};" +        // X-Achse Beschreibung
                            $"{this.Xaxis.factor.factor};" +      // X-Achse Faktor
                            $"{this.Xaxis.factor.signed};" +      // X-Achse Signiert
                            $"{this.Xaxis.factor.bitness};" +     // X-Achse Bitness
                            $"{this.Xaxis.factor.offset};" +      // X-Achse Offset
                            $"{this.Xaxis.factor.reciprocal};" +  // X-Achse Reziprok
                            $"{this.Xaxis.factor.prePrecision};" +// X-Achse Vor-Präzision
                            $"{this.Xaxis.factor.postPrecision};" +// X-Achse Nach-Präzision

                            // Y-Achse
                            $"{this.Yaxis.Length};" +             // Y-Achse Länge
                            $"{this.Yaxis.StartAddress};" +       // Y-Achse Startadresse
                            $"{this.Yaxis.Unit};" +               // Y-Achse Einheit
                            $"{this.Yaxis.Description};" +        // Y-Achse Beschreibung
                            $"{this.Yaxis.factor.factor};" +      // Y-Achse Faktor
                            $"{this.Yaxis.factor.signed};" +      // Y-Achse Signiert
                            $"{this.Yaxis.factor.bitness};" +     // Y-Achse Bitness
                            $"{this.Yaxis.factor.offset};" +      // Y-Achse Offset
                            $"{this.Yaxis.factor.reciprocal};" +  // Y-Achse Reziprok
                            $"{this.Yaxis.factor.prePrecision};" +// Y-Achse Vor-Präzision
                            $"{this.Yaxis.factor.postPrecision};";// Y-Achse Nach-Präzision
            return csvRow;
        }
        
        /// <summary>
        /// returns the header for CSV format
        /// </summary>
        /// <returns></returns>
        public string getCsvHeader()
        {
            return("Varname;NickName;Description;Unit;Id;StartAddress;Type;Length;Factor;Signed;Bitness;FactorOffset;Reciprocal;PrePrecision;PostPrecision;XaxisLength;XaxisStartAddress;XaxisUnit;XaxisDescription;XaxisFactor;XaxisSigned;XaxisBitness;XaxisOffset;XaxisReciprocal;XaxisPrePrecision;XaxisPostPrecision;YaxisLength;YaxisStartAddress;YaxisUnit;YaxisDescription;YaxisFactor;YaxisSigned;YaxisBitness;YaxisOffset;YaxisReciprocal;YaxisPrePrecision;YaxisPostPrecision;Category;Subcategory;Path");
        }
        
        #endregion
    }

    /// <summary>
    /// structure for axis description
    /// </summary>
    [MemoryPackable]
    public partial class SymbolAxis
    {
        #region description
        /// <summary>
        /// description of the axis [Engine RPM]
        /// </summary>
        public string  Description  { get; set; } = string.Empty;
        /// <summary>
        /// unit of the axis [1/min]
        /// </summary>
        public string Unit          { get; set; } = string.Empty;
        /// <summary>
        /// id from ecu description, like zmwMKOR_KF
        /// </summary>
        public string Id            { get; set; } = string.Empty;
        public int StartAddress     { get; set; } = 0;
        public int Length           { get; set; } = 0;

        #endregion

        /// <summary>
        /// factor to calculate axis
        /// </summary>
        public Ffactor factor = new Ffactor();



        #region legacy support
        /// <summary>
        /// lagacy do not use
        /// </summary>
        public int ID { get; set; } = 0;

        public bool assigned { get; set; } = false; 

        #endregion

    }

    /// <summary>
    /// structure for factor, bitness, etc.
    /// </summary>
    [MemoryPackable]
    public partial class Ffactor
    {
        #region data organisation
        public bool     signed      { get; set; } = false;
        public Bitness  bitness     { get; set; } = Bitness.Bit8;
        #endregion

        public double   factor          { get; set; } = 1;
        public double   offset          { get; set; } = 0;
        public double   reciprocalFactor { get; set; } = 1;
        public bool     reciprocal      { get; set; } = false;
        public int      prePrecision    { get; set; } = -1;
        public int      postPrecision   { get; set; } = 2;

    }
}
